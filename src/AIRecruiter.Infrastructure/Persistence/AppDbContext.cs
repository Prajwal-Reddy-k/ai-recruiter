using AIRecruiter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<RecruiterProfile> RecruiterProfiles => Set<RecruiterProfile>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<SavedJob> SavedJobs => Set<SavedJob>();
    public DbSet<JobAlert> JobAlerts => Set<JobAlert>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<JobTemplate> JobTemplates => Set<JobTemplate>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<InterviewFeedback> InterviewFeedbacks => Set<InterviewFeedback>();
    public DbSet<InterviewFeedbackEditHistory> InterviewFeedbackEditHistories => Set<InterviewFeedbackEditHistory>();
    public DbSet<JobAssignment> JobAssignments => Set<JobAssignment>();
    public DbSet<InterviewAssignment> InterviewAssignments => Set<InterviewAssignment>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<CandidateWorkExperience> CandidateWorkExperiences => Set<CandidateWorkExperience>();
    public DbSet<CandidateEducation> CandidateEducations => Set<CandidateEducation>();
    public DbSet<CandidateCertification> CandidateCertifications => Set<CandidateCertification>();
    public DbSet<CandidateProject> CandidateProjects => Set<CandidateProject>();
    public DbSet<CoverLetterTemplate> CoverLetterTemplates => Set<CoverLetterTemplate>();
    public DbSet<SkillAssessmentQuestion> SkillAssessmentQuestions => Set<SkillAssessmentQuestion>();
    public DbSet<SkillAssessmentAttempt> SkillAssessmentAttempts => Set<SkillAssessmentAttempt>();
    public DbSet<SkillAssessmentAnswer> SkillAssessmentAnswers => Set<SkillAssessmentAnswer>();
    public DbSet<CareerGoal> CareerGoals => Set<CareerGoal>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferStatusHistory> OfferStatusHistories => Set<OfferStatusHistory>();
    public DbSet<TalentPool> TalentPools => Set<TalentPool>();
    public DbSet<TalentPoolCandidate> TalentPoolCandidates => Set<TalentPoolCandidate>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<CompanyFollow> CompanyFollows => Set<CompanyFollow>();
    public DbSet<CompanyReview> CompanyReviews => Set<CompanyReview>();
    public DbSet<JobScreeningQuestion> JobScreeningQuestions => Set<JobScreeningQuestion>();
    public DbSet<ScreeningQuestionOption> ScreeningQuestionOptions => Set<ScreeningQuestionOption>();
    public DbSet<ScreeningAnswer> ScreeningAnswers => Set<ScreeningAnswer>();
    public DbSet<ScreeningAnswerSelectedOption> ScreeningAnswerSelectedOptions => Set<ScreeningAnswerSelectedOption>();
    public DbSet<JobView> JobViews => Set<JobView>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.CandidateProfile)
            .WithOne(c => c.User)
            .HasForeignKey<CandidateProfile>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.RecruiterProfile)
            .WithOne(r => r.User)
            .HasForeignKey<RecruiterProfile>(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RecruiterProfile>()
            .HasOne(r => r.Company)
            .WithMany(c => c.Recruiters)
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobPosting>()
            .HasOne(j => j.Company)
            .WithMany(c => c.JobPostings)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobPosting>()
            .HasOne(j => j.RecruiterProfile)
            .WithMany(r => r.JobPostings)
            .HasForeignKey(j => j.RecruiterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobApplication>()
            .HasOne(a => a.JobPosting)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobApplication>()
            .HasOne(a => a.CandidateProfile)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobApplication>()
            .HasIndex(a => new { a.JobPostingId, a.CandidateProfileId })
            .IsUnique();

        // --- Application status history ---
        modelBuilder.Entity<ApplicationStatusHistory>()
            .HasOne(h => h.JobApplication)
            .WithMany(a => a.StatusHistory)
            .HasForeignKey(h => h.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationStatusHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Notifications ---
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Interviews ---
        modelBuilder.Entity<Interview>()
            .HasOne(i => i.JobApplication)
            .WithMany(a => a.Interviews)
            .HasForeignKey(i => i.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Interview>()
            .HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Saved jobs ---
        modelBuilder.Entity<SavedJob>()
            .HasOne(s => s.CandidateProfile)
            .WithMany(c => c.SavedJobs)
            .HasForeignKey(s => s.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SavedJob>()
            .HasOne(s => s.JobPosting)
            .WithMany(j => j.SavedByCandidates)
            .HasForeignKey(s => s.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SavedJob>()
            .HasIndex(s => new { s.CandidateProfileId, s.JobPostingId })
            .IsUnique();

        // --- Job alerts ---
        modelBuilder.Entity<JobAlert>()
            .HasOne(a => a.CandidateProfile)
            .WithMany(c => c.JobAlerts)
            .HasForeignKey(a => a.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Reports (polymorphic: EntityType+EntityId, no hard FK to the reported entity) ---
        modelBuilder.Entity<Report>()
            .HasOne(r => r.ReportedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Report>()
            .HasIndex(r => new { r.EntityType, r.EntityId });

        // --- Password reset codes ---
        modelBuilder.Entity<PasswordResetCode>()
            .HasOne(p => p.User)
            .WithMany(u => u.PasswordResetCodes)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PasswordResetCode>()
            .HasIndex(p => new { p.UserId, p.IsUsed });

        // --- Audit log ---
        modelBuilder.Entity<AuditLogEntry>()
            .HasOne(e => e.ActorUser)
            .WithMany()
            .HasForeignKey(e => e.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Job templates ---
        modelBuilder.Entity<JobTemplate>()
            .HasOne(t => t.Company)
            .WithMany(c => c.JobTemplates)
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobTemplate>()
            .HasOne(t => t.RecruiterProfile)
            .WithMany()
            .HasForeignKey(t => t.RecruiterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Messages ---
        modelBuilder.Entity<Message>()
            .HasOne(m => m.JobApplication)
            .WithMany(a => a.Messages)
            .HasForeignKey(m => m.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.SenderUser)
            .WithMany()
            .HasForeignKey(m => m.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Interview feedback ---
        modelBuilder.Entity<InterviewFeedback>()
            .HasOne(f => f.Interview)
            .WithMany(i => i.Feedback)
            .HasForeignKey(f => f.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InterviewFeedback>()
            .HasOne(f => f.RecruiterProfile)
            .WithMany()
            .HasForeignKey(f => f.RecruiterProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InterviewFeedback>()
            .HasIndex(f => new { f.InterviewId, f.RecruiterProfileId })
            .IsUnique();

        modelBuilder.Entity<InterviewFeedbackEditHistory>()
            .HasOne(h => h.InterviewFeedback)
            .WithMany(f => f.EditHistory)
            .HasForeignKey(h => h.InterviewFeedbackId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InterviewFeedbackEditHistory>()
            .HasOne(h => h.EditedByUser)
            .WithMany()
            .HasForeignKey(h => h.EditedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Job / interview assignments ---
        modelBuilder.Entity<JobAssignment>()
            .HasOne(a => a.JobPosting)
            .WithMany(j => j.Assignments)
            .HasForeignKey(a => a.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobAssignment>()
            .HasOne(a => a.RecruiterProfile)
            .WithMany(r => r.JobAssignments)
            .HasForeignKey(a => a.RecruiterProfileId)
            // Restrict, not Cascade — SQL Server rejects a second cascade path here (the
            // other is JobPosting -> JobAssignment). TeamService.RemoveMemberAsync already
            // deletes a member's assignments explicitly before removing their profile.
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobAssignment>()
            .HasOne(a => a.AssignedByUser)
            .WithMany()
            .HasForeignKey(a => a.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobAssignment>()
            .HasIndex(a => new { a.JobPostingId, a.RecruiterProfileId })
            .IsUnique();

        modelBuilder.Entity<InterviewAssignment>()
            .HasOne(a => a.Interview)
            .WithMany(i => i.Assignments)
            .HasForeignKey(a => a.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InterviewAssignment>()
            .HasOne(a => a.RecruiterProfile)
            .WithMany(r => r.InterviewAssignments)
            .HasForeignKey(a => a.RecruiterProfileId)
            // Restrict, not Cascade — same multiple-cascade-path reason as JobAssignment.
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InterviewAssignment>()
            .HasOne(a => a.AssignedByUser)
            .WithMany()
            .HasForeignKey(a => a.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InterviewAssignment>()
            .HasIndex(a => new { a.InterviewId, a.RecruiterProfileId })
            .IsUnique();

        // --- Invitations ---
        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.JobPosting)
            .WithMany(j => j.Invitations)
            .HasForeignKey(i => i.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.CandidateProfile)
            .WithMany()
            .HasForeignKey(i => i.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.InvitedByUser)
            .WithMany()
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invitation>()
            .HasIndex(i => new { i.JobPostingId, i.CandidateProfileId, i.Status });

        // --- Feedback / support submissions ---
        modelBuilder.Entity<Feedback>()
            .HasOne(f => f.SubmittedByUser)
            .WithMany()
            .HasForeignKey(f => f.SubmittedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // --- Notification preferences (one-to-one, lazily created) ---
        modelBuilder.Entity<NotificationPreference>()
            .HasOne(p => p.User)
            .WithOne()
            .HasForeignKey<NotificationPreference>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotificationPreference>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        // --- Resume builder (all cascade-deleted with the owning candidate profile) ---
        modelBuilder.Entity<CandidateWorkExperience>()
            .HasOne(e => e.CandidateProfile)
            .WithMany(c => c.WorkExperiences)
            .HasForeignKey(e => e.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CandidateEducation>()
            .HasOne(e => e.CandidateProfile)
            .WithMany(c => c.ResumeEducations)
            .HasForeignKey(e => e.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CandidateCertification>()
            .HasOne(e => e.CandidateProfile)
            .WithMany(c => c.Certifications)
            .HasForeignKey(e => e.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CandidateProject>()
            .HasOne(e => e.CandidateProfile)
            .WithMany(c => c.Projects)
            .HasForeignKey(e => e.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CandidateProfile>()
            .HasIndex(c => c.PublicProfileSlug)
            .IsUnique()
            .HasFilter("[PublicProfileSlug] IS NOT NULL");

        // --- Cover letter templates (cascade-deleted with the owning candidate profile) ---
        modelBuilder.Entity<CoverLetterTemplate>()
            .HasOne(e => e.CandidateProfile)
            .WithMany(c => c.CoverLetterTemplates)
            .HasForeignKey(e => e.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Skill assessments ---
        modelBuilder.Entity<SkillAssessmentQuestion>()
            .HasIndex(q => q.Category);

        modelBuilder.Entity<SkillAssessmentAttempt>()
            .HasOne(a => a.CandidateProfile)
            .WithMany(c => c.AssessmentAttempts)
            .HasForeignKey(a => a.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SkillAssessmentAnswer>()
            .HasOne(a => a.Attempt)
            .WithMany(a => a.Answers)
            .HasForeignKey(a => a.SkillAssessmentAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SkillAssessmentAnswer>()
            .HasOne(a => a.Question)
            .WithMany()
            .HasForeignKey(a => a.SkillAssessmentQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Career goals (cascade-deleted with the owning candidate profile) ---
        modelBuilder.Entity<CareerGoal>()
            .HasOne(g => g.CandidateProfile)
            .WithMany(c => c.CareerGoals)
            .HasForeignKey(g => g.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Offers ---
        modelBuilder.Entity<Offer>()
            .HasOne(o => o.JobApplication)
            .WithMany(a => a.Offers)
            .HasForeignKey(o => o.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Offer>()
            .HasOne(o => o.CreatedByUser)
            .WithMany()
            .HasForeignKey(o => o.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Offer>()
            .HasIndex(o => new { o.JobApplicationId, o.Status });

        modelBuilder.Entity<OfferStatusHistory>()
            .HasOne(h => h.Offer)
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(h => h.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OfferStatusHistory>()
            .HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Talent pools (company-wide resource, not a personal one) ---
        modelBuilder.Entity<TalentPool>()
            .HasOne(p => p.Company)
            .WithMany(c => c.TalentPools)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TalentPool>()
            .HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TalentPoolCandidate>()
            .HasOne(c => c.TalentPool)
            .WithMany(p => p.Candidates)
            .HasForeignKey(c => c.TalentPoolId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TalentPoolCandidate>()
            .HasOne(c => c.CandidateProfile)
            .WithMany()
            // Restrict — defensive; no candidate hard-delete path exists today, but avoid a
            // second cascade path off CandidateProfile alongside TalentPool -> Candidate.
            .HasForeignKey(c => c.CandidateProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TalentPoolCandidate>()
            .HasOne(c => c.AddedByUser)
            .WithMany()
            .HasForeignKey(c => c.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TalentPoolCandidate>()
            .HasIndex(c => new { c.TalentPoolId, c.CandidateProfileId })
            .IsUnique();

        // --- Referrals ---
        modelBuilder.Entity<Referral>()
            .HasOne(r => r.ReferrerUser)
            .WithMany()
            .HasForeignKey(r => r.ReferrerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Referral>()
            .HasOne(r => r.JobPosting)
            .WithMany(j => j.Referrals)
            .HasForeignKey(r => r.JobPostingId)
            // Restrict, not Cascade — SQL Server rejects a second cascade path here (the
            // other is JobPosting -> JobApplication -> Referral via JobApplicationId/SetNull).
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Referral>()
            .HasOne(r => r.RegisteredUser)
            .WithMany()
            .HasForeignKey(r => r.RegisteredUserId)
            // Restrict — a second FK to User on the same row as ReferrerUserId; Restrict
            // avoids any multi-cascade-path ambiguity.
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Referral>()
            .HasOne(r => r.JobApplication)
            .WithMany()
            // SetNull — losing the linked application shouldn't delete the referral record.
            .HasForeignKey(r => r.JobApplicationId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Referral>()
            .HasIndex(r => r.TokenHash)
            .IsUnique();

        modelBuilder.Entity<Referral>()
            .HasIndex(r => new { r.ReferredEmail, r.JobPostingId })
            .IsUnique();

        // --- Company follows (candidate follows a company) ---
        modelBuilder.Entity<CompanyFollow>()
            .HasOne(f => f.CandidateProfile)
            .WithMany(c => c.FollowedCompanies)
            .HasForeignKey(f => f.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompanyFollow>()
            .HasOne(f => f.Company)
            .WithMany(c => c.Followers)
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompanyFollow>()
            .HasIndex(f => new { f.CandidateProfileId, f.CompanyId })
            .IsUnique();

        // --- Company verification ---
        modelBuilder.Entity<Company>()
            .HasOne(c => c.VerificationReviewedByUser)
            .WithMany()
            .HasForeignKey(c => c.VerificationReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Company reviews (anonymous-to-company, one per candidate per company) ---
        modelBuilder.Entity<CompanyReview>()
            .HasOne(r => r.Company)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompanyReview>()
            .HasOne(r => r.CandidateProfile)
            .WithMany(c => c.CompanyReviews)
            .HasForeignKey(r => r.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompanyReview>()
            .HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompanyReview>()
            .HasOne(r => r.RecruiterResponseByUser)
            .WithMany()
            .HasForeignKey(r => r.RecruiterResponseByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompanyReview>()
            .HasIndex(r => new { r.CandidateProfileId, r.CompanyId })
            .IsUnique();

        // --- Job screening questions (recruiter-authored) & candidate answers ---
        modelBuilder.Entity<JobScreeningQuestion>()
            .HasOne(q => q.JobPosting)
            .WithMany(j => j.ScreeningQuestions)
            .HasForeignKey(q => q.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScreeningQuestionOption>()
            .HasOne(o => o.JobScreeningQuestion)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.JobScreeningQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScreeningAnswer>()
            .HasOne(a => a.JobApplication)
            .WithMany(a => a.ScreeningAnswers)
            .HasForeignKey(a => a.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict (not Cascade) — mirrors SkillAssessmentAnswer.Question exactly, so a
        // question that already has an answer can never be deleted at the DB level, backing
        // up the service-level check in JobPostingService.SyncScreeningQuestionsAsync.
        modelBuilder.Entity<ScreeningAnswer>()
            .HasOne(a => a.JobScreeningQuestion)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.JobScreeningQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScreeningAnswer>()
            .HasIndex(a => new { a.JobApplicationId, a.JobScreeningQuestionId })
            .IsUnique();

        modelBuilder.Entity<ScreeningAnswerSelectedOption>()
            .HasOne(o => o.ScreeningAnswer)
            .WithMany(a => a.SelectedOptions)
            .HasForeignKey(o => o.ScreeningAnswerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ScreeningAnswerSelectedOption>()
            .HasOne(o => o.ScreeningQuestionOption)
            .WithMany()
            .HasForeignKey(o => o.ScreeningQuestionOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Recently-viewed jobs (real per-candidate history, distinct from the anonymous
        // JobPosting.ViewCount aggregate) ---
        modelBuilder.Entity<JobView>()
            .HasOne(v => v.CandidateProfile)
            .WithMany(c => c.JobViews)
            .HasForeignKey(v => v.CandidateProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobView>()
            .HasOne(v => v.JobPosting)
            .WithMany(j => j.JobViews)
            .HasForeignKey(v => v.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobView>()
            .HasIndex(v => new { v.CandidateProfileId, v.JobPostingId })
            .IsUnique();

        // --- Refresh tokens (rotation chain; reuse-of-rotated-token triggers theft detection) ---
        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(t => t.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => new { t.UserId, t.RevokedAtUtc });

        // Concurrency token (not a rowversion column) — a race between two redemption attempts
        // of the same token both read RevokedAtUtc as null; only the first save that sets it
        // wins, the second hits DbUpdateConcurrencyException. See RefreshTokenService.RedeemAsync.
        modelBuilder.Entity<RefreshToken>()
            .Property(t => t.RevokedAtUtc)
            .IsConcurrencyToken();

        // --- Performance indexes (purely additive — no existing index/column touched).
        // Each backs a specific .Where/.OrderBy pair found in the real service code; see
        // the AddPerformanceIndexes migration for the full rationale. ---
        modelBuilder.Entity<JobPosting>()
            .HasIndex(j => new { j.Status, j.ModerationStatus, j.CreatedAt });

        modelBuilder.Entity<JobPosting>()
            .HasIndex(j => new { j.CompanyId, j.Status, j.ModerationStatus, j.CreatedAt });

        modelBuilder.Entity<JobPosting>()
            .HasIndex(j => new { j.RecruiterProfileId, j.CreatedAt });

        modelBuilder.Entity<JobApplication>()
            .HasIndex(a => new { a.JobPostingId, a.CreatedAt });

        modelBuilder.Entity<JobApplication>()
            .HasIndex(a => new { a.CandidateProfileId, a.CreatedAt });

        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.JobApplicationId, m.CreatedAt });

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.CreatedAt });

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead });

        modelBuilder.Entity<Interview>()
            .HasIndex(i => new { i.JobApplicationId, i.CreatedAt });

        modelBuilder.Entity<Interview>()
            .HasIndex(i => new { i.Status, i.ScheduledStartUtc });

        modelBuilder.Entity<AuditLogEntry>()
            .HasIndex(e => new { e.ActorUserId, e.TimestampUtc });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }
        }
    }
}
