using AIRecruiter.Application.DTOs.Offers;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Digital offer-letter workflow layered on top of JobApplication. Deliberately
/// writes JobApplication.Status directly on Accept/Decline (rather than calling
/// IJobApplicationService.UpdateStatusAsync, which requires a recruiter caller for its
/// ownership check) since the acting user here is the candidate, whose ownership of the
/// underlying application this service has already verified independently.</summary>
public class OfferService : IOfferService
{
    private static readonly OfferStatus[] InactiveStatuses = { OfferStatus.Declined, OfferStatus.Withdrawn, OfferStatus.Expired };
    private static readonly ApplicationStatus[] FinalApplicationStatuses = { ApplicationStatus.Hired, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn };

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAuditLogService _auditLog;

    public OfferService(AppDbContext db, INotificationService notifications, IAuditLogService auditLog)
    {
        _db = db;
        _notifications = notifications;
        _auditLog = auditLog;
    }

    public async Task<OfferDto> CreateDraftAsync(int recruiterUserId, int jobApplicationId, CreateOfferRequest request, CancellationToken ct = default)
    {
        var application = await LoadApplicationForRecruiterAsync(recruiterUserId, jobApplicationId, ct);
        Validate(request.OfferedSalary, request.SalaryType, request.WorkCity, request.WorkState, request.IsRemote, request.EmploymentType, request.JoiningDate, request.ExpiryDateUtc);

        if (FinalApplicationStatuses.Contains(application.Status))
        {
            throw new ConflictException("APPLICATION_FINAL", $"This application is already {application.Status} — an offer can no longer be created.");
        }

        var hasActiveOffer = await _db.Offers.AnyAsync(o => o.JobApplicationId == jobApplicationId && !InactiveStatuses.Contains(o.Status), ct);
        if (hasActiveOffer)
        {
            throw new ConflictException("OFFER_ALREADY_ACTIVE", "An active offer already exists for this application.");
        }

        var offer = new Offer
        {
            JobApplicationId = jobApplicationId,
            CreatedByUserId = recruiterUserId,
            OfferedSalary = request.OfferedSalary,
            SalaryType = Enum.Parse<SalaryType>(request.SalaryType),
            JoiningDate = request.JoiningDate,
            WorkCity = request.WorkCity?.Trim(),
            WorkState = request.WorkState?.Trim(),
            IsRemote = request.IsRemote,
            EmploymentType = Enum.Parse<JobType>(request.EmploymentType),
            ProbationDetails = request.ProbationDetails?.Trim(),
            Benefits = request.Benefits?.Trim(),
            ExpiryDateUtc = request.ExpiryDateUtc,
            RecruiterMessage = request.RecruiterMessage?.Trim(),
            Status = OfferStatus.Draft,
        };
        _db.Offers.Add(offer);
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "OfferCreated", "Offer", offer.Id, new { application.JobPosting.Title }, ct);

        return await ToDtoAsync(offer.Id, ct);
    }

    public async Task<OfferDto> UpdateDraftAsync(int recruiterUserId, int offerId, UpdateOfferRequest request, CancellationToken ct = default)
    {
        var offer = await LoadOwnedOfferForRecruiterAsync(recruiterUserId, offerId, ct);
        Validate(request.OfferedSalary, request.SalaryType, request.WorkCity, request.WorkState, request.IsRemote, request.EmploymentType, request.JoiningDate, request.ExpiryDateUtc);

        if (offer.Status != OfferStatus.Draft)
        {
            throw new ConflictException("OFFER_NOT_DRAFT", "Only a draft offer can be edited.");
        }

        offer.OfferedSalary = request.OfferedSalary;
        offer.SalaryType = Enum.Parse<SalaryType>(request.SalaryType);
        offer.JoiningDate = request.JoiningDate;
        offer.WorkCity = request.WorkCity?.Trim();
        offer.WorkState = request.WorkState?.Trim();
        offer.IsRemote = request.IsRemote;
        offer.EmploymentType = Enum.Parse<JobType>(request.EmploymentType);
        offer.ProbationDetails = request.ProbationDetails?.Trim();
        offer.Benefits = request.Benefits?.Trim();
        offer.ExpiryDateUtc = request.ExpiryDateUtc;
        offer.RecruiterMessage = request.RecruiterMessage?.Trim();
        offer.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(offer.Id, ct);
    }

    public async Task<OfferDto> SendAsync(int recruiterUserId, int offerId, CancellationToken ct = default)
    {
        var offer = await LoadOwnedOfferForRecruiterAsync(recruiterUserId, offerId, ct);
        if (offer.Status != OfferStatus.Draft)
        {
            throw new ConflictException("OFFER_NOT_DRAFT", "Only a draft offer can be sent.");
        }

        await ChangeStatusAsync(offer, OfferStatus.Sent, recruiterUserId, null, ct);
        offer.SentAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var application = await _db.JobApplications.Include(a => a.CandidateProfile).Include(a => a.JobPosting)
            .FirstAsync(a => a.Id == offer.JobApplicationId, ct);
        await _notifications.NotifyAsync(
            application.CandidateProfile.UserId, "OfferSent",
            $"You've received a job offer for {application.JobPosting.Title}.", "Offer", offer.Id, ct);
        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "OfferSent", "Offer", offer.Id, new { application.JobPosting.Title }, ct);

        return await ToDtoAsync(offer.Id, ct);
    }

    public async Task<OfferDto> WithdrawAsync(int recruiterUserId, int offerId, CancellationToken ct = default)
    {
        var offer = await LoadOwnedOfferForRecruiterAsync(recruiterUserId, offerId, ct);
        if (InactiveStatuses.Contains(offer.Status) || offer.Status == OfferStatus.Accepted)
        {
            throw new ConflictException("OFFER_ALREADY_DECIDED", $"This offer is already {offer.Status} and cannot be withdrawn.");
        }

        await ChangeStatusAsync(offer, OfferStatus.Withdrawn, recruiterUserId, null, ct);
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "OfferWithdrawn", "Offer", offer.Id, null, ct);

        return await ToDtoAsync(offer.Id, ct);
    }

    public async Task<OfferDetailDto> GetDetailAsync(int userId, string role, int offerId, CancellationToken ct = default)
    {
        var offer = await LoadOfferWithAccessCheckAsync(userId, role, offerId, ct);

        var history = await _db.OfferStatusHistories
            .Where(h => h.OfferId == offerId)
            .Include(h => h.ChangedByUser)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new OfferStatusHistoryEntryDto(h.FromStatus.HasValue ? h.FromStatus.Value.ToString() : null, h.ToStatus.ToString(), h.ChangedByUser.FullName, h.ChangedAt, h.Note))
            .ToListAsync(ct);

        return new OfferDetailDto(await ToDtoAsync(offer.Id, ct), history);
    }

    public async Task<IReadOnlyList<OfferDto>> GetForApplicationAsync(int userId, string role, int jobApplicationId, CancellationToken ct = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.CandidateProfile)
            .FirstOrDefaultAsync(a => a.Id == jobApplicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        await EnsureCanAccessApplicationAsync(application, userId, role, ct);

        var offerIds = await _db.Offers.Where(o => o.JobApplicationId == jobApplicationId).OrderByDescending(o => o.CreatedAt).Select(o => o.Id).ToListAsync(ct);
        var results = new List<OfferDto>();
        foreach (var id in offerIds)
        {
            results.Add(await ToDtoAsync(id, ct));
        }
        return results;
    }

    public async Task<IReadOnlyList<OfferDto>> GetMyOffersAsync(int candidateUserId, CancellationToken ct = default)
    {
        var offerIds = await _db.Offers
            .Where(o => o.JobApplication.CandidateProfile.UserId == candidateUserId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => o.Id)
            .ToListAsync(ct);

        var results = new List<OfferDto>();
        foreach (var id in offerIds)
        {
            results.Add(await ToDtoAsync(id, ct));
        }
        return results;
    }

    public async Task<OfferDto> RespondAsync(int candidateUserId, int offerId, RespondToOfferRequest request, CancellationToken ct = default)
    {
        var offer = await _db.Offers
            .Include(o => o.JobApplication).ThenInclude(a => a.CandidateProfile)
            .Include(o => o.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(o => o.Id == offerId, ct)
            ?? throw new NotFoundException("Offer not found.");

        if (offer.JobApplication.CandidateProfile.UserId != candidateUserId)
        {
            throw new ForbiddenException("You do not have access to this offer.");
        }

        if (offer.ExpiryDateUtc < DateTime.UtcNow)
        {
            throw new ConflictException("OFFER_EXPIRED", "This offer has expired.");
        }

        if (offer.Status != OfferStatus.Sent && offer.Status != OfferStatus.Viewed)
        {
            throw new ConflictException("OFFER_ALREADY_DECIDED", $"This offer is already {offer.Status}.");
        }

        var application = offer.JobApplication;
        var newOfferStatus = request.Accept ? OfferStatus.Accepted : OfferStatus.Declined;
        var note = request.Accept ? "Offer accepted" : ("Offer declined" + (string.IsNullOrWhiteSpace(request.Note) ? "" : $": {request.Note.Trim()}"));

        await ChangeStatusAsync(offer, newOfferStatus, candidateUserId, request.Note?.Trim(), ct);
        offer.RespondedAtUtc = DateTime.UtcNow;
        offer.CandidateResponseNote = request.Note?.Trim();

        var previousAppStatus = application.Status;
        application.Status = request.Accept ? ApplicationStatus.Hired : ApplicationStatus.Rejected;
        application.UpdatedAt = DateTime.UtcNow;
        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            JobApplicationId = application.Id,
            FromStatus = previousAppStatus,
            ToStatus = application.Status,
            ChangedByUserId = candidateUserId,
            Note = note,
        });

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.JobPosting.RecruiterProfile.UserId,
            request.Accept ? "OfferAccepted" : "OfferDeclined",
            $"{note} for {application.JobPosting.Title}.",
            "Offer", offer.Id, ct);
        await _auditLog.LogAsync(candidateUserId, "Candidate", request.Accept ? "OfferAccepted" : "OfferDeclined", "Offer", offer.Id, new { application.JobPosting.Title }, ct);

        return await ToDtoAsync(offer.Id, ct);
    }

    private async Task<JobApplication> LoadApplicationForRecruiterAsync(int recruiterUserId, int jobApplicationId, CancellationToken ct)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(a => a.Id == jobApplicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this application.");
        }

        return application;
    }

    private async Task<Offer> LoadOwnedOfferForRecruiterAsync(int recruiterUserId, int offerId, CancellationToken ct)
    {
        var offer = await _db.Offers
            .Include(o => o.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(o => o.Id == offerId, ct)
            ?? throw new NotFoundException("Offer not found.");

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, offer.JobApplication.JobPosting.RecruiterProfile.UserId, offer.JobApplication.JobPosting.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this offer.");
        }

        return offer;
    }

    private async Task<Offer> LoadOfferWithAccessCheckAsync(int userId, string role, int offerId, CancellationToken ct)
    {
        var offer = await _db.Offers
            .Include(o => o.JobApplication).ThenInclude(a => a.CandidateProfile)
            .Include(o => o.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(o => o.Id == offerId, ct)
            ?? throw new NotFoundException("Offer not found.");

        await EnsureCanAccessApplicationAsync(offer.JobApplication, userId, role, ct);
        return offer;
    }

    private async Task EnsureCanAccessApplicationAsync(JobApplication application, int userId, string role, CancellationToken ct)
    {
        var isOwningCandidate = role == "Candidate" && application.CandidateProfile.UserId == userId;
        var isOwningRecruiter = role == "Recruiter" && await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(
            _db, userId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct);

        if (!isOwningCandidate && !isOwningRecruiter)
        {
            throw new ForbiddenException("You do not have access to this offer.");
        }
    }

    private async Task ChangeStatusAsync(Offer offer, OfferStatus newStatus, int changedByUserId, string? note, CancellationToken ct)
    {
        var previous = offer.Status;
        offer.Status = newStatus;
        offer.UpdatedAt = DateTime.UtcNow;
        _db.OfferStatusHistories.Add(new OfferStatusHistory
        {
            OfferId = offer.Id,
            FromStatus = previous,
            ToStatus = newStatus,
            ChangedByUserId = changedByUserId,
            Note = note,
        });
        await Task.CompletedTask;
    }

    private static void Validate(decimal offeredSalary, string salaryType, string? workCity, string? workState, bool isRemote, string employmentType, DateTime joiningDate, DateTime expiryDateUtc)
    {
        var errors = new Dictionary<string, string>();

        if (offeredSalary <= 0) errors["offeredSalary"] = "Offered salary must be greater than zero.";
        if (!Enum.TryParse<SalaryType>(salaryType, out _)) errors["salaryType"] = "Salary type must be Monthly or Annual.";
        if (!Enum.TryParse<JobType>(employmentType, out _)) errors["employmentType"] = "Choose a valid employment type.";
        if (!isRemote && (string.IsNullOrWhiteSpace(workCity) || string.IsNullOrWhiteSpace(workState)))
        {
            errors["workCity"] = "Provide a work city and state, or mark this offer as Remote — India.";
        }
        if (expiryDateUtc <= DateTime.UtcNow) errors["expiryDateUtc"] = "Expiry date must be in the future.";

        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);
    }

    private async Task<OfferDto> ToDtoAsync(int offerId, CancellationToken ct)
    {
        var offer = await _db.Offers
            .Include(o => o.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(o => o.JobApplication).ThenInclude(a => a.CandidateProfile).ThenInclude(c => c.User)
            .FirstAsync(o => o.Id == offerId, ct);

        var application = offer.JobApplication;
        return new OfferDto(
            offer.Id, application.Id, application.JobPosting.Title, application.JobPosting.Company.Name, application.CandidateProfile.User.FullName,
            offer.OfferedSalary, offer.SalaryType.ToString(), offer.JoiningDate, offer.WorkCity, offer.WorkState, offer.IsRemote,
            offer.EmploymentType.ToString(), offer.ProbationDetails, offer.Benefits, offer.ExpiryDateUtc, offer.RecruiterMessage,
            offer.Status.ToString(), offer.SentAtUtc, offer.RespondedAtUtc, offer.CandidateResponseNote, offer.CreatedAt);
    }
}
