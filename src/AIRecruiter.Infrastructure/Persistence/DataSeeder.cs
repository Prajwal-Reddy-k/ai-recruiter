using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Persistence;

/// <summary>
/// Idempotent, Development-only demo data for the India-focused platform. Never wired up
/// outside Development (see Program.cs) so it can never run in a production environment.
/// Safe to call on every startup — a single existence check on the first seeded user
/// short-circuits the whole routine if it has already run.
/// </summary>
public static class DataSeeder
{
    private const string DemoPassword = "Demo@123";
    private const string EmailDomain = "@demo.airecruiter.dev";

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var alreadySeeded = await db.Users.AnyAsync(u => u.Email == $"recruiter1{EmailDomain}", ct);
        if (!alreadySeeded)
        {
            await SeedCoreDemoDataAsync(db, ct);
        }

        // Runs independently of the guard above, with its own idempotency check — added in a
        // later batch, after `recruiter1` (and everyone else above) may already exist in an
        // existing dev database from an earlier run. Without this, a database seeded before
        // this feature batch was added would never receive any of this demo content.
        await SeedFeatureBatch2DemoDataAsync(db, ct);
    }

    private static async Task SeedCoreDemoDataAsync(AppDbContext db, CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

        // --- Admin (seeded directly — self-registration as Admin is blocked) -----------
        db.Users.Add(new User { FullName = "Ops Admin", Email = $"admin{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Admin });

        // --- Companies (India HQs) ------------------------------------------------------
        var nimbus = new Company { Name = "Nimbus Cloud Systems", Industry = "Cloud Software", Website = "https://nimbus-cloud.example", Description = "Cloud infrastructure and platform tooling for mid-size engineering teams.", City = "Bengaluru", State = "Karnataka", Size = "201-500", Benefits = "Health insurance, ESOPs, hybrid work, annual learning budget", CultureHighlights = "Engineering-led, async-first, quarterly hackathons" };
        var bluepeak = new Company { Name = "BluePeak Analytics", Industry = "Data & Analytics", Website = "https://bluepeak-analytics.example", Description = "Data platforms and machine learning products for retail forecasting.", City = "Hyderabad", State = "Telangana", Size = "51-200", Benefits = "Health insurance, flexible hours, remote-friendly", CultureHighlights = "Data-driven decision making, weekly demo days" };
        var solstice = new Company { Name = "Solstice Retail Group", Industry = "E-commerce", Website = "https://solstice-retail.example", Description = "Omnichannel retail technology and storefront experiences.", City = "Pune", State = "Maharashtra", Size = "501-1000", Benefits = "Health insurance, employee discounts, relocation support", CultureHighlights = "Customer-obsessed, fast iteration cycles" };
        var vertex = new Company { Name = "Vertex FinTech Solutions", Industry = "FinTech", Website = "https://vertex-fintech.example", Description = "Payments infrastructure and lending products for Indian SMBs.", City = "Mumbai", State = "Maharashtra", Size = "201-500", Benefits = "Health insurance, performance bonus, gym membership", CultureHighlights = "Regulated, security-first, high ownership" };
        var northgate = new Company { Name = "Northgate Systems", Industry = "Enterprise Software", Website = "https://northgate-systems.example", Description = "B2B SaaS for supply chain and logistics operators.", City = "Chennai", State = "Tamil Nadu", Size = "51-200", Benefits = "Health insurance, provident fund, hybrid work", CultureHighlights = "Process-driven, customer support excellence" };
        var coral = new Company { Name = "Coral Health Informatics", Industry = "HealthTech", Website = "https://coral-health.example", Description = "Clinical data systems for hospitals and diagnostic chains.", City = "Kochi", State = "Kerala", Size = "51-200", Benefits = "Health insurance, wellness stipend, flexible leave", CultureHighlights = "Mission-driven, compliance-conscious, mentorship culture" };

        var recruiter1User = new User { FullName = "Priya Sharma", Email = $"recruiter1{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Recruiter };
        var recruiter2User = new User { FullName = "James Verghese", Email = $"recruiter2{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Recruiter };
        var recruiter3User = new User { FullName = "Meera Nair", Email = $"recruiter3{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Recruiter };
        var recruiter4User = new User { FullName = "Karan Mehta", Email = $"recruiter4{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Recruiter };
        var recruiter5User = new User { FullName = "Ananya Iyer", Email = $"recruiter5{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Recruiter };
        var recruiter6User = new User { FullName = "Rahul Menon", Email = $"recruiter6{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Recruiter };
        // A second team member at Solstice — demonstrates the hiring-team feature with real
        // seeded data (an Interviewer assigned to one specific interview, not the whole
        // company). Solstice because that's where the seeded InterviewCompleted application
        // (UI/UX Designer, candidate7) lives — the assignment must be same-company.
        var interviewer1User = new User { FullName = "Nikhil Rao", Email = $"interviewer1{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Recruiter };

        var recruiter1 = new RecruiterProfile { User = recruiter1User, Company = nimbus, Designation = "Talent Acquisition Lead", CompanyRole = CompanyRole.Owner };
        var recruiter2 = new RecruiterProfile { User = recruiter2User, Company = bluepeak, Designation = "HR Manager", CompanyRole = CompanyRole.Owner };
        var recruiter3 = new RecruiterProfile { User = recruiter3User, Company = solstice, Designation = "Recruiter", CompanyRole = CompanyRole.Owner };
        var recruiter4 = new RecruiterProfile { User = recruiter4User, Company = vertex, Designation = "Talent Partner", CompanyRole = CompanyRole.Owner };
        var recruiter5 = new RecruiterProfile { User = recruiter5User, Company = northgate, Designation = "Recruiter", CompanyRole = CompanyRole.Owner };
        var recruiter6 = new RecruiterProfile { User = recruiter6User, Company = coral, Designation = "People Operations", CompanyRole = CompanyRole.Owner };
        var interviewer1 = new RecruiterProfile { User = interviewer1User, Company = solstice, Designation = "Design Lead", CompanyRole = CompanyRole.Interviewer };

        db.RecruiterProfiles.AddRange(recruiter1, recruiter2, recruiter3, recruiter4, recruiter5, recruiter6, interviewer1);

        // --- Candidates -------------------------------------------------------------
        var candidate1User = new User { FullName = "Arjun Mehta", Email = $"candidate1{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate2User = new User { FullName = "Sara Kapoor", Email = $"candidate2{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate3User = new User { FullName = "Liam Fernandes", Email = $"candidate3{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate4User = new User { FullName = "Fatima Sheikh", Email = $"candidate4{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate5User = new User { FullName = "Rohan Gupta", Email = $"candidate5{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate6User = new User { FullName = "Divya Krishnan", Email = $"candidate6{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate7User = new User { FullName = "Aditi Shah", Email = $"candidate7{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        // Deliberately suspended — demonstrates the admin moderation/suspension feature with
        // real seeded data. This account cannot log in until an Admin reactivates it.
        var candidate8User = new User { FullName = "Vikram Singh", Email = $"candidate8{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate, IsActive = false };

        var candidate1 = new CandidateProfile { User = candidate1User, Headline = "Senior Backend Engineer", Summary = "Backend engineer focused on .NET services and cloud-native APIs.", Education = "B.Tech in Computer Science, IIT Bombay", ExperienceSummary = "4 years building and operating ASP.NET Core services at scale.", TotalExperienceYears = 4, City = "Bengaluru", State = "Karnataka", SkillsCsv = "C#, ASP.NET Core, SQL Server, Docker, Entity Framework Core", AvailabilityStatus = AvailabilityStatus.ActivelyLooking, ProfileVisibility = ProfileVisibility.VisibleToRecruiters, PreferredJobTypesCsv = "FullTime", PreferredLocationsCsv = "Bengaluru, Karnataka", RemotePreference = false, ExpectedSalaryMin = 1800000, ExpectedSalaryMax = 2400000, NoticePeriodDays = 30, PreferredRolesCsv = "Backend Engineer, Platform Engineer" };
        var candidate2 = new CandidateProfile { User = candidate2User, Headline = "Data Analyst", Summary = "Data analyst turning messy data into decisions.", Education = "B.Sc. Statistics, University of Hyderabad", ExperienceSummary = "2 years in retail analytics and dashboarding.", TotalExperienceYears = 2, City = "Hyderabad", State = "Telangana", SkillsCsv = "Python, SQL, Power BI, Pandas, Data Analysis", AvailabilityStatus = AvailabilityStatus.NotLooking, ProfileVisibility = ProfileVisibility.Private };
        var candidate3 = new CandidateProfile { User = candidate3User, Headline = "Frontend Developer", Summary = "Frontend developer specializing in React and design systems.", Education = "B.E. Computer Engineering, College of Engineering Pune", ExperienceSummary = "3 years shipping consumer-facing React applications.", TotalExperienceYears = 3, City = "Pune", State = "Maharashtra", SkillsCsv = "React, TypeScript, CSS, HTML, UI/UX" };
        var candidate4 = new CandidateProfile { User = candidate4User, Headline = "DevOps Engineer", Summary = "DevOps engineer automating infrastructure on AWS.", Education = "B.Tech Computer Engineering, VJTI Mumbai", ExperienceSummary = "5 years running Kubernetes clusters and CI/CD pipelines.", TotalExperienceYears = 5, City = "Mumbai", State = "Maharashtra", SkillsCsv = "AWS, Docker, Kubernetes, CI/CD, Terraform", AvailabilityStatus = AvailabilityStatus.ActivelyLooking, ProfileVisibility = ProfileVisibility.VisibleToRecruiters, PreferredJobTypesCsv = "FullTime, Contract", PreferredLocationsCsv = "Mumbai, Maharashtra", RemotePreference = true, ExpectedSalaryMin = 2200000, ExpectedSalaryMax = 3000000, NoticePeriodDays = 60, PreferredRolesCsv = "DevOps Engineer, SRE" };
        var candidate5 = new CandidateProfile { User = candidate5User, Headline = "QA Engineer", Summary = "QA engineer specializing in test automation.", Education = "B.Tech IT, Delhi Technological University", ExperienceSummary = "3 years building automated test suites.", TotalExperienceYears = 3, City = "Delhi NCR", State = "Delhi", SkillsCsv = "Selenium, Test Automation, C#, xUnit" };
        var candidate6 = new CandidateProfile { User = candidate6User, Headline = "Full Stack Developer", Summary = "Full stack developer across .NET and React.", Education = "B.E. Computer Science, Anna University", ExperienceSummary = "4 years across backend and frontend delivery.", TotalExperienceYears = 4, City = "Chennai", State = "Tamil Nadu", SkillsCsv = "C#, React, SQL Server, ASP.NET Core, TypeScript" };
        var candidate7 = new CandidateProfile { User = candidate7User, Headline = "UI/UX Designer", Summary = "Product designer focused on accessible, usable interfaces.", Education = "B.Des, National Institute of Design", ExperienceSummary = "3 years designing end-to-end product flows.", TotalExperienceYears = 3, City = "Ahmedabad", State = "Gujarat", SkillsCsv = "Figma, UI/UX, Adobe XD" };
        var candidate8 = new CandidateProfile
        {
            // Deliberately sparse — demonstrates the profile-completion card for a new signup.
            User = candidate8User,
            Headline = "Recent graduate looking for backend roles",
            City = "Jaipur",
            State = "Rajasthan",
        };

        db.CandidateProfiles.AddRange(candidate1, candidate2, candidate3, candidate4, candidate5, candidate6, candidate7, candidate8);

        await db.SaveChangesAsync(ct);

        // --- Jobs — spread across all 12 target cities plus one Remote listing ----------
        var jobSeniorBackend = new JobPosting { Title = "Senior Backend Engineer", Description = "Own core services powering our platform APIs. Strong C# and SQL Server experience required.", RequiredSkillsCsv = "C#, ASP.NET Core, SQL Server, Docker", MinExperienceYears = 3, City = "Bengaluru", State = "Karnataka", JobType = JobType.FullTime, Status = JobStatus.Open, Company = nimbus, RecruiterProfile = recruiter1 };
        var jobCloudDevOps = new JobPosting { Title = "Cloud DevOps Engineer", Description = "Build and operate our Kubernetes-based deployment pipelines across environments.", RequiredSkillsCsv = "AWS, Docker, Kubernetes, CI/CD", MinExperienceYears = 3, City = "Bengaluru", State = "Karnataka", JobType = JobType.FullTime, Status = JobStatus.Open, Company = nimbus, RecruiterProfile = recruiter1 };
        var jobSupportKolkata = new JobPosting { Title = "Support Engineer", Description = "Frontline technical support for our enterprise cloud customers.", RequiredSkillsCsv = "Linux, SQL Server, Communication", MinExperienceYears = 1, City = "Kolkata", State = "West Bengal", JobType = JobType.FullTime, Status = JobStatus.Closed, Company = nimbus, RecruiterProfile = recruiter1 };

        var jobDataAnalyst = new JobPosting { Title = "Data Analyst", Description = "Turn retail transaction data into forecasting insights for merchandising teams.", RequiredSkillsCsv = "Python, SQL, Power BI", MinExperienceYears = 1, City = "Hyderabad", State = "Telangana", JobType = JobType.FullTime, Status = JobStatus.Open, Company = bluepeak, RecruiterProfile = recruiter2 };
        var jobMlEngineer = new JobPosting { Title = "Machine Learning Engineer", Description = "Build demand-forecasting models and productionize them at scale. Fully remote role.", RequiredSkillsCsv = "Python, Machine Learning, Pandas", MinExperienceYears = 2, IsRemote = true, JobType = JobType.FullTime, Status = JobStatus.Open, Company = bluepeak, RecruiterProfile = recruiter2 };
        var jobDataInternJaipur = new JobPosting { Title = "Data Analyst Intern", Description = "Support the analytics team with dashboarding and reporting.", RequiredSkillsCsv = "SQL, Power BI", City = "Jaipur", State = "Rajasthan", JobType = JobType.Internship, Status = JobStatus.Open, Company = bluepeak, RecruiterProfile = recruiter2 };

        var jobFrontendReact = new JobPosting { Title = "Frontend Developer (React)", Description = "Build storefront experiences used by millions of shoppers.", RequiredSkillsCsv = "React, TypeScript, CSS", MinExperienceYears = 2, City = "Pune", State = "Maharashtra", JobType = JobType.FullTime, Status = JobStatus.Open, Company = solstice, RecruiterProfile = recruiter3 };
        var jobUiUxDesigner = new JobPosting { Title = "UI/UX Designer", Description = "Design end-to-end shopping flows in close partnership with engineering.", RequiredSkillsCsv = "Figma, UI/UX", MinExperienceYears = 1, City = "Pune", State = "Maharashtra", JobType = JobType.Contract, Status = JobStatus.Open, Company = solstice, RecruiterProfile = recruiter3 };
        var jobBusinessAnalystAhmedabad = new JobPosting { Title = "Business Analyst", Description = "Analyze warehouse operations data to improve fulfillment efficiency.", RequiredSkillsCsv = "SQL, Data Analysis", MinExperienceYears = 2, City = "Ahmedabad", State = "Gujarat", JobType = JobType.FullTime, Status = JobStatus.Open, Company = solstice, RecruiterProfile = recruiter3 };

        var jobBackendPayments = new JobPosting { Title = "Backend Engineer (Payments)", Description = "Build reliable payment processing services for Indian SMBs.", RequiredSkillsCsv = "C#, ASP.NET Core, SQL Server", MinExperienceYears = 3, City = "Mumbai", State = "Maharashtra", JobType = JobType.FullTime, Status = JobStatus.Open, Company = vertex, RecruiterProfile = recruiter4 };
        var jobQaMumbai = new JobPosting { Title = "QA Engineer", Description = "Own test automation for our lending and payments products.", RequiredSkillsCsv = "Selenium, Test Automation, C#", MinExperienceYears = 2, City = "Mumbai", State = "Maharashtra", JobType = JobType.FullTime, Status = JobStatus.Open, Company = vertex, RecruiterProfile = recruiter4 };
        var jobFrontendDelhi = new JobPosting { Title = "Frontend Engineer", Description = "Build the customer dashboard for our lending platform.", RequiredSkillsCsv = "React, TypeScript", MinExperienceYears = 2, City = "Delhi NCR", State = "Delhi", JobType = JobType.FullTime, Status = JobStatus.Open, Company = vertex, RecruiterProfile = recruiter4 };

        var jobFullStackChennai = new JobPosting { Title = "Full Stack Developer", Description = "Build supply-chain visibility features end to end.", RequiredSkillsCsv = "C#, React, SQL Server", MinExperienceYears = 3, City = "Chennai", State = "Tamil Nadu", JobType = JobType.FullTime, Status = JobStatus.Open, Company = northgate, RecruiterProfile = recruiter5 };
        var jobSupportChennai = new JobPosting { Title = "Customer Support Engineer", Description = "Part-time technical support for logistics operator accounts.", RequiredSkillsCsv = "Communication, SQL", City = "Chennai", State = "Tamil Nadu", JobType = JobType.PartTime, Status = JobStatus.Open, Company = northgate, RecruiterProfile = recruiter5 };
        var jobQaIndore = new JobPosting { Title = "QA Engineer (Contract)", Description = "6-month contract to build out our regression test suite.", RequiredSkillsCsv = "Test Automation, Selenium", City = "Indore", State = "Madhya Pradesh", JobType = JobType.Contract, Status = JobStatus.Open, Company = northgate, RecruiterProfile = recruiter5 };

        var jobBackendHealthKochi = new JobPosting { Title = "Backend Engineer (Healthcare)", Description = "Build HIPAA-conscious clinical data services.", RequiredSkillsCsv = "C#, ASP.NET Core, SQL Server", MinExperienceYears = 2, City = "Kochi", State = "Kerala", JobType = JobType.FullTime, Status = JobStatus.Open, Company = coral, RecruiterProfile = recruiter6 };
        var jobDataEngIntern = new JobPosting { Title = "Data Engineering Intern", Description = "Assist with clinical data pipeline development.", RequiredSkillsCsv = "SQL, Python", City = "Kochi", State = "Kerala", JobType = JobType.Internship, Status = JobStatus.Open, Company = coral, RecruiterProfile = recruiter6 };
        var jobBackendCoimbatore = new JobPosting { Title = "Backend Engineer", Description = "Build diagnostic reporting services for our Coimbatore delivery center.", RequiredSkillsCsv = "C#, SQL Server", MinExperienceYears = 1, City = "Coimbatore", State = "Tamil Nadu", JobType = JobType.FullTime, Status = JobStatus.Open, Company = coral, RecruiterProfile = recruiter6 };

        var allJobs = new[]
        {
            jobSeniorBackend, jobCloudDevOps, jobSupportKolkata,
            jobDataAnalyst, jobMlEngineer, jobDataInternJaipur,
            jobFrontendReact, jobUiUxDesigner, jobBusinessAnalystAhmedabad,
            jobBackendPayments, jobQaMumbai, jobFrontendDelhi,
            jobFullStackChennai, jobSupportChennai, jobQaIndore,
            jobBackendHealthKochi, jobDataEngIntern, jobBackendCoimbatore,
        };
        db.JobPostings.AddRange(allJobs);
        await db.SaveChangesAsync(ct);

        // A hidden listing so the admin moderation view has something to show.
        jobSupportKolkata.ModerationStatus = ModerationStatus.Hidden;

        // Application deadlines — one expiring within a few days (demonstrates the
        // "expiring soon" dashboard warning), one further out.
        jobSeniorBackend.ApplicationDeadlineUtc = DateTime.UtcNow.AddDays(2);
        jobFrontendReact.ApplicationDeadlineUtc = DateTime.UtcNow.AddDays(21);

        // --- Applications ----------------------------------------------------------------
        var appSeniorBackendCandidate1 = new JobApplication { JobPosting = jobSeniorBackend, CandidateProfile = candidate1, Status = ApplicationStatus.Shortlisted };
        var appDataAnalystCandidate2 = new JobApplication { JobPosting = jobDataAnalyst, CandidateProfile = candidate2, Status = ApplicationStatus.InterviewScheduled };
        var appUiUxCandidate7 = new JobApplication { JobPosting = jobUiUxDesigner, CandidateProfile = candidate7, Status = ApplicationStatus.InterviewCompleted };

        db.JobApplications.AddRange(
            appSeniorBackendCandidate1,
            new JobApplication { JobPosting = jobCloudDevOps, CandidateProfile = candidate1, Status = ApplicationStatus.Applied },
            appDataAnalystCandidate2,
            new JobApplication { JobPosting = jobMlEngineer, CandidateProfile = candidate2, Status = ApplicationStatus.Screening },
            new JobApplication { JobPosting = jobFrontendReact, CandidateProfile = candidate3, Status = ApplicationStatus.Hired },
            new JobApplication { JobPosting = jobUiUxDesigner, CandidateProfile = candidate3, Status = ApplicationStatus.Rejected },
            new JobApplication { JobPosting = jobBackendPayments, CandidateProfile = candidate4, Status = ApplicationStatus.Shortlisted },
            new JobApplication { JobPosting = jobQaMumbai, CandidateProfile = candidate5, Status = ApplicationStatus.Applied },
            new JobApplication { JobPosting = jobFrontendDelhi, CandidateProfile = candidate5, Status = ApplicationStatus.Screening },
            new JobApplication { JobPosting = jobFullStackChennai, CandidateProfile = candidate6, Status = ApplicationStatus.Offer },
            new JobApplication { JobPosting = jobSupportChennai, CandidateProfile = candidate6, Status = ApplicationStatus.Applied },
            appUiUxCandidate7,
            new JobApplication { JobPosting = jobBackendHealthKochi, CandidateProfile = candidate8, Status = ApplicationStatus.Applied },
            new JobApplication { JobPosting = jobSeniorBackend, CandidateProfile = candidate6, Status = ApplicationStatus.Withdrawn },
            new JobApplication { JobPosting = jobDataAnalyst, CandidateProfile = candidate1, Status = ApplicationStatus.Rejected });

        await db.SaveChangesAsync(ct);

        // Reports across three of the four reportable entity types now (Message follows once
        // a real message exists below) — the admin review queue demonstrates the full
        // moderation workflow out of the box.
        db.Reports.AddRange(
            new Report
            {
                EntityType = ReportedEntityType.Job,
                EntityId = jobSupportKolkata.Id,
                ReportedByUserId = candidate1User.Id,
                Reason = ReportReason.Other,
                Details = "Listing appears stale — role was filled weeks ago.",
            },
            new Report
            {
                EntityType = ReportedEntityType.Company,
                EntityId = northgate.Id,
                ReportedByUserId = candidate6User.Id,
                Reason = ReportReason.FakeCompany,
                Details = "Couldn't find this company on LinkedIn or their own website.",
            },
            new Report
            {
                EntityType = ReportedEntityType.User,
                EntityId = recruiter5User.Id,
                ReportedByUserId = candidate5User.Id,
                Reason = ReportReason.Harassment,
                Details = "Recruiter kept messaging after I asked them to stop.",
                Status = ReportStatus.UnderReview,
            });

        // --- Interviews — backfills a pre-existing gap where InterviewScheduled/Completed
        // application statuses had no real Interview rows behind them. -------------------
        var interviewScheduled = new Interview
        {
            JobApplication = appDataAnalystCandidate2,
            ScheduledStartUtc = DateTime.UtcNow.AddDays(3),
            ScheduledEndUtc = DateTime.UtcNow.AddDays(3).AddMinutes(45),
            Type = InterviewType.Online,
            Location = "https://meet.example.com/bluepeak-data-analyst",
            RecruiterNote = "30 min technical + 15 min culture fit.",
            Status = InterviewStatus.Scheduled,
            CreatedByUser = recruiter2User,
        };
        var interviewCompleted = new Interview
        {
            JobApplication = appUiUxCandidate7,
            ScheduledStartUtc = DateTime.UtcNow.AddDays(-2),
            ScheduledEndUtc = DateTime.UtcNow.AddDays(-2).AddMinutes(45),
            Type = InterviewType.Online,
            Location = "https://meet.example.com/solstice-uiux-designer",
            RecruiterNote = "Portfolio walkthrough + design exercise.",
            Status = InterviewStatus.Completed,
            CreatedByUser = recruiter3User,
        };
        db.Interviews.AddRange(interviewScheduled, interviewCompleted);
        await db.SaveChangesAsync(ct);

        // Assign the Interviewer teammate to the completed interview, and give them (and
        // the Owner) a submitted scorecard for it.
        db.InterviewAssignments.Add(new InterviewAssignment
        {
            Interview = interviewCompleted,
            RecruiterProfile = interviewer1,
            AssignedByUserId = recruiter3User.Id,
        });

        db.InterviewFeedbacks.Add(new InterviewFeedback
        {
            Interview = interviewCompleted,
            RecruiterProfile = interviewer1,
            TechnicalScore = 4,
            CommunicationScore = 5,
            ProblemSolvingScore = 4,
            CultureFitScore = 5,
            Recommendation = InterviewRecommendation.Yes,
            Strengths = "Strong portfolio, clear communication of design decisions.",
            Concerns = "Limited experience with design-system tooling.",
            PrivateNotes = "Would pair well with the frontend team.",
            IsDraft = false,
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-2).AddHours(1),
        });

        // --- A short message thread on one application ------------------------------------
        var messageFromRecruiter = new Message { JobApplication = appSeniorBackendCandidate1, SenderUser = recruiter1User, SenderRole = "Recruiter", Body = "Hi Arjun, thanks for applying! Are you available for a quick call this week?" };
        db.Messages.AddRange(
            messageFromRecruiter,
            new Message { JobApplication = appSeniorBackendCandidate1, SenderUser = candidate1User, SenderRole = "Candidate", Body = "Hi Priya, yes — I'm free Wednesday or Thursday afternoon IST." });
        await db.SaveChangesAsync(ct);

        // Fourth reportable entity type (Message) — needs a real message id, so it's added
        // once the thread above has been persisted.
        db.Reports.Add(new Report
        {
            EntityType = ReportedEntityType.Message,
            EntityId = messageFromRecruiter.Id,
            ReportedByUserId = candidate1User.Id,
            Reason = ReportReason.Spam,
            Details = "This looks like an automated message.",
            Status = ReportStatus.Resolved,
            ModerationNote = "Reviewed — message was legitimate, no action taken.",
        });

        // --- Recruiter candidate invitations ------------------------------------------------
        // candidate4 opted into VisibleToRecruiters, so any recruiter can discover and invite
        // them even though they haven't applied anywhere yet.
        db.Invitations.Add(new Invitation
        {
            JobPosting = jobBackendPayments,
            CandidateProfile = candidate4,
            InvitedByUserId = recruiter4User.Id,
            Message = "Hi Fatima — your DevOps background looks like a great fit for our Payments platform team. Would you be open to applying?",
            Status = InvitationStatus.Sent,
            SentAtUtc = DateTime.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(13),
        });
        db.Invitations.Add(new Invitation
        {
            JobPosting = jobSeniorBackend,
            CandidateProfile = candidate1,
            InvitedByUserId = recruiter1User.Id,
            Message = "Arjun, since you've already applied for Senior Backend Engineer, wanted to flag our Cloud DevOps opening too in case it's a better fit.",
            Status = InvitationStatus.Accepted,
            SentAtUtc = DateTime.UtcNow.AddDays(-5),
            ViewedAtUtc = DateTime.UtcNow.AddDays(-4),
            RespondedAtUtc = DateTime.UtcNow.AddDays(-4),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(9),
        });

        // --- A reusable job template for Nimbus --------------------------------------------
        db.JobTemplates.Add(new JobTemplate
        {
            Company = nimbus,
            RecruiterProfile = recruiter1,
            Title = "Backend Engineer",
            Department = "Engineering",
            Description = "Own core services powering our platform APIs.",
            Responsibilities = "Design and build APIs, review code, participate in on-call rotation.",
            RequiredSkillsCsv = "C#, ASP.NET Core, SQL Server",
            PreferredSkillsCsv = "Docker, Kubernetes",
            MinExperienceYears = 2,
            MaxExperienceYears = 6,
            EmploymentType = JobType.FullTime,
            SalaryVisible = false,
            DefaultCity = "Bengaluru",
            DefaultState = "Karnataka",
            DefaultIsRemote = false,
        });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Feature-batch-2 demo content (cover letters, skill assessment question bank
    /// + sample attempts, a public-shareable candidate, career goals). Guarded independently
    /// of <see cref="SeedCoreDemoDataAsync"/> so it also backfills into a database that was
    /// already seeded before this batch existed. Looks candidate1/candidate3 up by email
    /// rather than taking them as parameters, since they may come from either this run or a
    /// previous one.</summary>
    private static async Task SeedFeatureBatch2DemoDataAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.SkillAssessmentQuestions.AnyAsync(ct))
        {
            return;
        }

        var candidate1 = await db.CandidateProfiles.Include(c => c.User).FirstAsync(c => c.User.Email == $"candidate1{EmailDomain}", ct);
        var candidate3 = await db.CandidateProfiles.Include(c => c.User).FirstAsync(c => c.User.Email == $"candidate3{EmailDomain}", ct);

        // --- Skill assessment question bank — locally authored, deliberately fictional
        // trivia-style questions, not sourced from or affiliated with any real certification
        // body. ~15 per category, enough depth for the 15-question/attempt format. -----------
        db.SkillAssessmentQuestions.AddRange(BuildQuestionBank());
        await db.SaveChangesAsync(ct);

        // --- Feature batch 2 demo content: cover letters, a sample assessment attempt (one
        // opted into recruiter visibility, one not), a public-shareable candidate, and career
        // goals across all three statuses. ----------------------------------------------------
        db.CoverLetterTemplates.AddRange(
            new CoverLetterTemplate
            {
                CandidateProfile = candidate1,
                Title = "Backend roles",
                Introduction = "I'm a backend engineer with 4 years of experience building ASP.NET Core services.",
                SkillsHighlights = "C#, ASP.NET Core, SQL Server, Docker, Entity Framework Core.",
                ProjectAchievements = "Led the migration of a monolith to a service-oriented ASP.NET Core architecture, cutting deploy times by 60%.",
                ClosingMessage = "I'd welcome the chance to discuss how my background fits this role.",
            },
            new CoverLetterTemplate
            {
                CandidateProfile = candidate1,
                Title = "Platform / DevOps-adjacent roles",
                Introduction = "Backend engineer with hands-on Docker and CI/CD experience alongside core service development.",
                SkillsHighlights = "C#, Docker, SQL Server, CI/CD pipelines.",
                ProjectAchievements = "Containerized and automated deployment for a 12-service backend platform.",
                ClosingMessage = "Happy to share more detail in an interview.",
            });

        // candidate1 opts into being publicly shareable — a stable, seeded slug (not
        // generated) so the demo public URL doesn't change across reseeds.
        candidate1.ProfileVisibility = ProfileVisibility.PublicShareable;
        candidate1.PublicProfileSlug = "arjun-mehta-demo1";

        db.CareerGoals.AddRange(
            new CareerGoal
            {
                CandidateProfile = candidate1,
                TargetRole = "Staff Backend Engineer",
                TargetSkill = "Kubernetes",
                PreferredState = "Karnataka",
                PreferredCity = "Bengaluru",
                TargetCompletionDate = DateTime.UtcNow.AddMonths(9),
                ProgressPercent = 35,
                Status = CareerGoalStatus.InProgress,
                Notes = "Focusing on distributed-systems depth before applying to staff-level roles.",
            },
            new CareerGoal
            {
                CandidateProfile = candidate1,
                TargetRole = "Backend Engineer",
                TargetCompanyType = "FinTech",
                ProgressPercent = 100,
                Status = CareerGoalStatus.Completed,
                Notes = "Landed the Senior Backend Engineer role at Nimbus — goal achieved.",
            },
            new CareerGoal
            {
                CandidateProfile = candidate3,
                TargetSkill = "GraphQL",
                TargetRole = "Senior Frontend Developer",
                ProgressPercent = 10,
                Status = CareerGoalStatus.Paused,
                Notes = "Paused while focusing on the current job search.",
            });

        await db.SaveChangesAsync(ct);

        var javaQuestionIds = await db.SkillAssessmentQuestions.Where(q => q.Category == AssessmentCategory.Java).Select(q => q.Id).Take(15).ToListAsync(ct);
        var reactQuestionIds = await db.SkillAssessmentQuestions.Where(q => q.Category == AssessmentCategory.React).Select(q => q.Id).Take(15).ToListAsync(ct);

        // candidate1: a strong, recruiter-visible Java result — demonstrates the opted-in badge
        // on both the recruiter candidate-search detail view and the public portfolio profile.
        var candidate1Attempt = new SkillAssessmentAttempt
        {
            CandidateProfile = candidate1,
            Category = AssessmentCategory.Java,
            Status = AssessmentAttemptStatus.Completed,
            StartedAt = DateTime.UtcNow.AddDays(-10).AddMinutes(-20),
            SubmittedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-10),
            ScoreCorrectCount = 13,
            TotalQuestionCount = javaQuestionIds.Count,
            PercentageScore = javaQuestionIds.Count > 0 ? Math.Round(13m / javaQuestionIds.Count * 100, 1) : 0,
            IsVisibleToRecruiters = true,
        };
        db.SkillAssessmentAttempts.Add(candidate1Attempt);

        // candidate3: a completed React result kept private by default — demonstrates that an
        // attempt exists but stays invisible to recruiters until the candidate opts in.
        var candidate3Attempt = new SkillAssessmentAttempt
        {
            CandidateProfile = candidate3,
            Category = AssessmentCategory.React,
            Status = AssessmentAttemptStatus.Completed,
            StartedAt = DateTime.UtcNow.AddDays(-3).AddMinutes(-20),
            SubmittedAt = DateTime.UtcNow.AddDays(-3),
            ExpiresAt = DateTime.UtcNow.AddDays(-3),
            ScoreCorrectCount = 10,
            TotalQuestionCount = reactQuestionIds.Count,
            PercentageScore = reactQuestionIds.Count > 0 ? Math.Round(10m / reactQuestionIds.Count * 100, 1) : 0,
            IsVisibleToRecruiters = false,
        };
        db.SkillAssessmentAttempts.Add(candidate3Attempt);

        await db.SaveChangesAsync(ct);

        for (var i = 0; i < javaQuestionIds.Count; i++)
        {
            db.SkillAssessmentAnswers.Add(new SkillAssessmentAnswer
            {
                SkillAssessmentAttemptId = candidate1Attempt.Id,
                SkillAssessmentQuestionId = javaQuestionIds[i],
                DisplayOrder = i,
                SelectedOptionIndex = 0,
            });
        }
        for (var i = 0; i < reactQuestionIds.Count; i++)
        {
            db.SkillAssessmentAnswers.Add(new SkillAssessmentAnswer
            {
                SkillAssessmentAttemptId = candidate3Attempt.Id,
                SkillAssessmentQuestionId = reactQuestionIds[i],
                DisplayOrder = i,
                SelectedOptionIndex = 0,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static SkillAssessmentQuestion Q(AssessmentCategory category, string text, string a, string b, string c, string d, int correct, string? explanation = null) =>
        new() { Category = category, QuestionText = text, OptionA = a, OptionB = b, OptionC = c, OptionD = d, CorrectOptionIndex = correct, Explanation = explanation };

    /// <summary>Locally authored, fictional MCQ question bank — not sourced from or affiliated
    /// with any real certification body. ~15 questions per category.</summary>
    private static List<SkillAssessmentQuestion> BuildQuestionBank()
    {
        var questions = new List<SkillAssessmentQuestion>();

        questions.AddRange(new[]
        {
            Q(AssessmentCategory.Java, "Which keyword is used to inherit a class in Java?", "extends", "implements", "inherits", "super", 0),
            Q(AssessmentCategory.Java, "Which collection type does not allow duplicate elements?", "ArrayList", "LinkedList", "HashSet", "Vector", 2),
            Q(AssessmentCategory.Java, "What does the JVM stand for?", "Java Virtual Machine", "Java Variable Method", "Java Verified Module", "Java Version Manager", 0),
            Q(AssessmentCategory.Java, "Which keyword makes a variable's value unchangeable after assignment?", "static", "final", "const", "readonly", 1),
            Q(AssessmentCategory.Java, "Which of these is NOT a primitive type in Java?", "int", "boolean", "String", "char", 2),
            Q(AssessmentCategory.Java, "What is the default value of a boolean instance variable?", "true", "false", "null", "0", 1),
            Q(AssessmentCategory.Java, "Which interface must a class implement to be used in a for-each loop?", "Iterable", "Comparable", "Serializable", "Cloneable", 0),
            Q(AssessmentCategory.Java, "Which exception is thrown when dividing an integer by zero?", "NullPointerException", "ArithmeticException", "NumberFormatException", "ClassCastException", 1),
            Q(AssessmentCategory.Java, "What is the purpose of the 'synchronized' keyword?", "Improve performance", "Control access from multiple threads", "Declare a constant", "Import a package", 1),
            Q(AssessmentCategory.Java, "Which method is the entry point of a Java application?", "start()", "run()", "main()", "init()", 2),
            Q(AssessmentCategory.Java, "Which of these best describes an abstract class?", "Cannot be instantiated directly", "Cannot have any methods", "Must be final", "Can't have constructors", 0),
            Q(AssessmentCategory.Java, "What does 'garbage collection' manage in Java?", "Compiled bytecode", "Automatic memory reclamation", "Thread scheduling", "Network sockets", 1),
            Q(AssessmentCategory.Java, "Which access modifier makes a member visible only within its own class?", "public", "protected", "private", "default", 2),
            Q(AssessmentCategory.Java, "What is autoboxing in Java?", "Automatic conversion between primitives and wrapper classes", "Automatic exception handling", "Automatic class loading", "Automatic garbage collection", 0),
            Q(AssessmentCategory.Java, "Which keyword is used to handle exceptions that might be thrown in a block of code?", "catch", "throw", "try", "finally", 2),
        });

        questions.AddRange(new[]
        {
            Q(AssessmentCategory.CSharpDotNet, "Which keyword declares a nullable value type in C#?", "null", "?", "Nullable<T> or T?", "optional", 2),
            Q(AssessmentCategory.CSharpDotNet, "What does LINQ stand for?", "Language Integrated Query", "Linked Interface Query", "Logical Index Query", "List Integration Query", 0),
            Q(AssessmentCategory.CSharpDotNet, "Which of these is used for dependency injection registration in ASP.NET Core?", "AppSettings.json", "IServiceCollection", "Program.ini", "web.config", 1),
            Q(AssessmentCategory.CSharpDotNet, "What is the purpose of the 'async'/'await' keywords?", "Multi-threading only", "Asynchronous, non-blocking code execution", "Compile-time constants", "Memory management", 1),
            Q(AssessmentCategory.CSharpDotNet, "Which collection interface represents a read-only sequence of elements?", "IList<T>", "ICollection<T>", "IEnumerable<T>", "IDictionary<T,V>", 2),
            Q(AssessmentCategory.CSharpDotNet, "Which keyword is used to define an interface in C#?", "interface", "abstract", "class", "struct", 0),
            Q(AssessmentCategory.CSharpDotNet, "What is Entity Framework Core?", "A logging framework", "An object-relational mapper (ORM)", "A testing framework", "A dependency injection container", 1),
            Q(AssessmentCategory.CSharpDotNet, "Which lifetime means a new instance is created every time it's requested in ASP.NET Core DI?", "Singleton", "Scoped", "Transient", "Static", 2),
            Q(AssessmentCategory.CSharpDotNet, "What does 'var' do when declaring a variable in C#?", "Declares a dynamic (runtime-typed) variable", "Declares a variable with a compile-time inferred type", "Declares a global variable", "Declares a constant", 1),
            Q(AssessmentCategory.CSharpDotNet, "Which attribute marks a class as not to be instantiated?", "[Sealed]", "[Abstract]", "[Static]", "[NotInstantiable]", 1),
            Q(AssessmentCategory.CSharpDotNet, "What is the purpose of a middleware component in ASP.NET Core?", "Store application secrets", "Process HTTP requests/responses in a pipeline", "Define database schemas", "Compile Razor views", 1),
            Q(AssessmentCategory.CSharpDotNet, "Which keyword prevents a class from being inherited?", "final", "sealed", "static", "readonly", 1),
            Q(AssessmentCategory.CSharpDotNet, "What is a record type primarily used for in modern C#?", "Mutable reference types with identity semantics", "Immutable data with value-based equality", "Only for database entities", "Replacing all classes", 1),
            Q(AssessmentCategory.CSharpDotNet, "Which HTTP status code does ASP.NET Core's [Authorize] typically trigger for an unauthenticated request?", "200", "401", "404", "500", 1),
            Q(AssessmentCategory.CSharpDotNet, "What does the 'using' statement/declaration ensure?", "The object is deterministically disposed", "The object is serialized", "The object is cached", "The object is thread-safe", 0),
        });

        questions.AddRange(new[]
        {
            Q(AssessmentCategory.React, "What is the purpose of the useState hook?", "Perform side effects", "Manage local component state", "Access the DOM directly", "Define routes", 1),
            Q(AssessmentCategory.React, "What does JSX compile down to?", "HTML strings", "React.createElement calls", "CSS modules", "WebAssembly", 1),
            Q(AssessmentCategory.React, "Which hook is used to perform side effects like data fetching?", "useMemo", "useEffect", "useRef", "useCallback", 1),
            Q(AssessmentCategory.React, "What is the 'key' prop used for when rendering lists?", "Styling list items", "Helping React identify which items changed", "Setting the list's default value", "Sorting the list", 1),
            Q(AssessmentCategory.React, "What is a 'controlled component' in React forms?", "A component whose value is managed by React state", "A component rendered only on the server", "A component that cannot re-render", "A third-party UI library component", 0),
            Q(AssessmentCategory.React, "What does React Context primarily solve?", "Routing between pages", "Passing data through the component tree without prop drilling", "Handling HTTP requests", "Compiling TypeScript", 1),
            Q(AssessmentCategory.React, "Which lifecycle concept does useEffect's cleanup function relate to class components?", "componentDidMount only", "componentWillUnmount / componentDidUpdate cleanup", "shouldComponentUpdate", "getDerivedStateFromProps", 1),
            Q(AssessmentCategory.React, "What is the virtual DOM?", "A copy of the browser's DOM stored in memory for diffing", "A server-rendered HTML page", "A CSS-in-JS engine", "A React DevTools panel", 0),
            Q(AssessmentCategory.React, "What does useMemo primarily help with?", "Memoizing expensive computed values", "Managing global state", "Making HTTP requests", "Handling form submissions", 0),
            Q(AssessmentCategory.React, "What is 'prop drilling'?", "Passing props through many nested components unnecessarily", "A React performance optimization", "A form validation technique", "A routing pattern", 0),
            Q(AssessmentCategory.React, "Which hook lets you access a mutable value that persists across renders without causing a re-render?", "useState", "useRef", "useEffect", "useContext", 1),
            Q(AssessmentCategory.React, "What is the recommended way to update state based on the previous state?", "Directly mutate the state variable", "Use the functional updater form, e.g. setCount(c => c + 1)", "Call setState twice in a row", "Use a global variable", 1),
            Q(AssessmentCategory.React, "What does React.memo do?", "Prevents a component from re-rendering if its props haven't changed", "Caches API responses", "Minifies component code", "Lazy-loads a component", 0),
            Q(AssessmentCategory.React, "Which of these is a valid reason to use useCallback?", "To memoize a function reference between renders", "To fetch data on mount", "To manage component state", "To define CSS styles", 0),
            Q(AssessmentCategory.React, "What does 'lifting state up' mean in React?", "Moving shared state to the closest common ancestor component", "Using Redux instead of local state", "Moving a component higher in the file", "Increasing a state value", 0),
        });

        questions.AddRange(new[]
        {
            Q(AssessmentCategory.JavaScript, "Which keyword declares a block-scoped variable in JavaScript?", "var", "let", "global", "def", 1),
            Q(AssessmentCategory.JavaScript, "What does '===' check for that '==' does not?", "Nothing, they're identical", "Type as well as value equality", "Only reference equality for objects", "Case-insensitive comparison", 1),
            Q(AssessmentCategory.JavaScript, "What is a Promise used for?", "Synchronous looping", "Representing the eventual result of an asynchronous operation", "Declaring constants", "Styling elements", 1),
            Q(AssessmentCategory.JavaScript, "What does 'this' refer to inside a regular (non-arrow) function called as a method?", "The global object always", "The object the method was called on", "undefined always", "The function itself", 1),
            Q(AssessmentCategory.JavaScript, "What is the output of typeof null?", "'null'", "'undefined'", "'object'", "'boolean'", 2),
            Q(AssessmentCategory.JavaScript, "Which method converts a JSON string into a JavaScript object?", "JSON.stringify()", "JSON.parse()", "Object.fromJSON()", "JSON.toObject()", 1),
            Q(AssessmentCategory.JavaScript, "What is a closure?", "A function bundled with its lexical scope", "A loop that never terminates", "A syntax error", "A CSS selector", 0),
            Q(AssessmentCategory.JavaScript, "Which array method creates a new array with the results of calling a function on every element?", "forEach", "map", "filter", "reduce", 1),
            Q(AssessmentCategory.JavaScript, "What does the spread operator (...) do when used on an array?", "Removes duplicate elements", "Expands array elements into individual elements", "Sorts the array", "Reverses the array", 1),
            Q(AssessmentCategory.JavaScript, "Which of these is an example of an arrow function?", "function() {}", "() => {}", "function*() {}", "async function() {}", 1),
            Q(AssessmentCategory.JavaScript, "What does 'hoisting' refer to in JavaScript?", "Variable and function declarations being moved to the top of their scope during compilation", "Moving code to a CDN", "Optimizing loops automatically", "A CSS layout technique", 0),
            Q(AssessmentCategory.JavaScript, "Which built-in object is used to handle asynchronous operations with .then()/.catch()?", "Array", "Promise", "Map", "Set", 1),
            Q(AssessmentCategory.JavaScript, "What does Array.prototype.reduce() do?", "Filters elements matching a condition", "Reduces an array to a single accumulated value", "Sorts an array in place", "Removes the last element", 1),
            Q(AssessmentCategory.JavaScript, "What is the purpose of 'use strict'?", "Enables stricter parsing and error handling in JavaScript", "Imports a strict-typed library", "Disables all warnings", "Enables ES6 modules", 0),
            Q(AssessmentCategory.JavaScript, "What does NaN stand for?", "Not a Number", "Null and None", "New Array Node", "Numeric Array Notation", 0),
        });

        questions.AddRange(new[]
        {
            Q(AssessmentCategory.Sql, "Which SQL clause is used to filter rows before grouping?", "HAVING", "WHERE", "GROUP BY", "ORDER BY", 1),
            Q(AssessmentCategory.Sql, "Which SQL clause filters groups after a GROUP BY?", "WHERE", "HAVING", "FILTER", "ON", 1),
            Q(AssessmentCategory.Sql, "Which JOIN returns only matching rows from both tables?", "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "FULL OUTER JOIN", 2),
            Q(AssessmentCategory.Sql, "Which JOIN returns all rows from the left table, and matched rows from the right?", "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "CROSS JOIN", 0),
            Q(AssessmentCategory.Sql, "What does the SQL 'PRIMARY KEY' constraint guarantee?", "Values can repeat", "Uniqueness and non-null identification of a row", "Automatic indexing is disabled", "The column must be text", 1),
            Q(AssessmentCategory.Sql, "Which statement is used to remove all rows from a table but keep its structure?", "DROP TABLE", "DELETE FROM (no WHERE) or TRUNCATE TABLE", "REMOVE TABLE", "CLEAR TABLE", 1),
            Q(AssessmentCategory.Sql, "What does a FOREIGN KEY enforce?", "A column must be unique", "Referential integrity between two tables", "A column can't be null", "Automatic sorting", 1),
            Q(AssessmentCategory.Sql, "Which function returns the number of rows matching a query?", "SUM()", "COUNT()", "TOTAL()", "LEN()", 1),
            Q(AssessmentCategory.Sql, "What is the purpose of an index in a database table?", "Enforce foreign keys", "Speed up data retrieval at the cost of write overhead", "Encrypt column data", "Automatically back up the table", 1),
            Q(AssessmentCategory.Sql, "Which SQL keyword combines the result sets of two queries and removes duplicates?", "UNION ALL", "UNION", "JOIN", "MERGE", 1),
            Q(AssessmentCategory.Sql, "What does 'normalization' aim to reduce in database design?", "Query speed", "Data redundancy and inconsistency", "The number of tables", "Index usage", 1),
            Q(AssessmentCategory.Sql, "Which SQL command is used to change the structure of an existing table?", "UPDATE TABLE", "MODIFY TABLE", "ALTER TABLE", "CHANGE TABLE", 2),
            Q(AssessmentCategory.Sql, "What does a transaction's 'ACID' property 'Atomicity' guarantee?", "All operations in a transaction complete or none do", "Data is always consistent across replicas", "Transactions run in isolation from each other", "Committed data survives crashes", 0),
            Q(AssessmentCategory.Sql, "Which clause is used to sort query results?", "SORT BY", "ORDER BY", "GROUP BY", "ARRANGE BY", 1),
            Q(AssessmentCategory.Sql, "What is a stored procedure?", "A precompiled, reusable SQL routine stored in the database", "A backup of a table", "A type of index", "A view with write access", 0),
        });

        questions.AddRange(new[]
        {
            Q(AssessmentCategory.Python, "Which keyword defines a function in Python?", "func", "def", "function", "lambda", 1),
            Q(AssessmentCategory.Python, "Which data type is immutable in Python?", "list", "dict", "tuple", "set", 2),
            Q(AssessmentCategory.Python, "What does the 'self' parameter represent in a class method?", "The class itself", "The instance the method is called on", "A global variable", "The parent class", 1),
            Q(AssessmentCategory.Python, "Which of these is used to handle exceptions in Python?", "try/except", "catch/throw", "on error resume", "handle/raise", 0),
            Q(AssessmentCategory.Python, "What does list comprehension provide?", "A concise way to create lists", "A way to sort dictionaries", "A threading primitive", "A file I/O method", 0),
            Q(AssessmentCategory.Python, "Which built-in function returns the number of items in a collection?", "count()", "len()", "size()", "length()", 1),
            Q(AssessmentCategory.Python, "What is a Python decorator?", "A function that modifies the behavior of another function", "A type of loop", "A comment syntax", "A package manager command", 0),
            Q(AssessmentCategory.Python, "Which module is commonly used for data manipulation and analysis?", "pandas", "flask", "requests", "os", 0),
            Q(AssessmentCategory.Python, "What does the 'with' statement help manage?", "Loop iteration", "Resource setup/teardown (context management)", "Exception types", "Type hints", 1),
            Q(AssessmentCategory.Python, "Which keyword is used to import a specific function from a module?", "import", "from ... import", "include", "require", 1),
            Q(AssessmentCategory.Python, "What is the difference between a list and a tuple?", "Lists are mutable, tuples are immutable", "Tuples can only hold numbers", "Lists cannot be nested", "There is no difference", 0),
            Q(AssessmentCategory.Python, "What does 'PEP 8' refer to?", "A Python performance benchmark", "Python's style guide for code formatting", "A package installer", "A testing framework", 1),
            Q(AssessmentCategory.Python, "Which of these correctly opens a file for reading in Python?", "open('file.txt', 'r')", "read('file.txt')", "file.open('file.txt')", "File.load('file.txt')", 0),
            Q(AssessmentCategory.Python, "What does a generator function use to yield values lazily?", "return", "yield", "await", "emit", 1),
            Q(AssessmentCategory.Python, "Which built-in type represents an unordered collection of unique elements?", "list", "tuple", "set", "dict", 2),
        });

        questions.AddRange(new[]
        {
            Q(AssessmentCategory.Communication, "When giving constructive feedback, which approach is generally most effective?", "Focus on the person's character", "Be specific, focus on behavior, and suggest improvement", "Give feedback publicly to set an example", "Avoid giving feedback to prevent conflict", 1),
            Q(AssessmentCategory.Communication, "What is 'active listening'?", "Waiting for your turn to speak", "Fully concentrating on, understanding, and responding to a speaker", "Interrupting to clarify quickly", "Taking detailed notes only", 1),
            Q(AssessmentCategory.Communication, "In a professional email, what is generally the best practice for the subject line?", "Leave it blank", "Make it clear and specific to the email's content", "Use all capital letters for emphasis", "Keep it vague to encourage opening", 1),
            Q(AssessmentCategory.Communication, "When disagreeing with a colleague in a meeting, which approach is most constructive?", "Stay silent and raise it privately with others later", "Respectfully explain your perspective with reasoning", "Dismiss their point immediately", "Agree publicly but complain afterward", 1),
            Q(AssessmentCategory.Communication, "What does 'empathy' mean in a workplace communication context?", "Agreeing with everyone", "Understanding and being sensitive to others' feelings and perspectives", "Avoiding difficult conversations", "Focusing only on facts, not feelings", 1),
            Q(AssessmentCategory.Communication, "Which of these best describes 'nonverbal communication'?", "Written communication only", "Body language, tone, and facial expressions", "Communication over email", "Formal presentations only", 1),
            Q(AssessmentCategory.Communication, "What is the benefit of asking open-ended questions?", "They can be answered with yes/no", "They encourage detailed, thoughtful responses", "They save time in conversations", "They are easier to answer than closed questions", 1),
            Q(AssessmentCategory.Communication, "When presenting to stakeholders, what should generally come first?", "Every technical detail", "A clear summary of the key point or ask", "A long personal introduction", "A list of unrelated topics", 1),
            Q(AssessmentCategory.Communication, "What is a good practice when receiving critical feedback?", "Defend yourself immediately", "Listen fully, ask clarifying questions, and reflect before responding", "Dismiss it if you disagree", "Respond only in writing to avoid discussion", 1),
            Q(AssessmentCategory.Communication, "In written workplace communication, why is clarity important?", "It makes messages longer", "It reduces misunderstandings and follow-up questions", "It's only relevant for external communication", "It's less important than speed", 1),
            Q(AssessmentCategory.Communication, "What does 'tone' refer to in communication?", "The volume of your voice only", "The attitude or emotion conveyed through words and delivery", "The length of a message", "The font used in an email", 1),
            Q(AssessmentCategory.Communication, "When collaborating across time zones, what is a good communication practice?", "Assume everyone is available immediately", "Be explicit about deadlines and time zones, and document decisions", "Only use verbal communication", "Avoid written summaries", 1),
            Q(AssessmentCategory.Communication, "What is the purpose of a status update in a team setting?", "To show off individual work only", "To keep stakeholders informed of progress, blockers, and next steps", "To replace all meetings", "To assign blame for delays", 1),
            Q(AssessmentCategory.Communication, "Which is an example of a 'closed' question?", "\"What do you think about this approach?\"", "\"Did you finish the report?\"", "\"How did the meeting go?\"", "\"What are your thoughts on the plan?\"", 1),
            Q(AssessmentCategory.Communication, "Why is it useful to summarize key points at the end of a meeting?", "It wastes time", "It confirms shared understanding and next steps", "It's only needed for large meetings", "It replaces the need for meeting notes", 1),
        });

        questions.AddRange(new[]
        {
            Q(AssessmentCategory.Aptitude, "If a train travels 60 km in 45 minutes, what is its speed in km/h?", "60 km/h", "80 km/h", "75 km/h", "90 km/h", 1),
            Q(AssessmentCategory.Aptitude, "What comes next in the sequence: 2, 4, 8, 16, ?", "18", "24", "32", "20", 2),
            Q(AssessmentCategory.Aptitude, "If 5 workers can complete a task in 12 days, how many days will 10 workers take (same rate)?", "24 days", "6 days", "12 days", "3 days", 1),
            Q(AssessmentCategory.Aptitude, "A shop offers a 20% discount on a ₹500 item. What is the final price?", "₹450", "₹400", "₹480", "₹380", 1),
            Q(AssessmentCategory.Aptitude, "If A is taller than B, and B is taller than C, who is the shortest?", "A", "B", "C", "Cannot be determined", 2),
            Q(AssessmentCategory.Aptitude, "What is 15% of 200?", "20", "25", "30", "35", 2),
            Q(AssessmentCategory.Aptitude, "Complete the analogy: Book is to Reading as Fork is to ?", "Cooking", "Eating", "Kitchen", "Cutting", 1),
            Q(AssessmentCategory.Aptitude, "If today is Wednesday, what day will it be 100 days from now?", "Tuesday", "Wednesday", "Thursday", "Friday", 2),
            Q(AssessmentCategory.Aptitude, "A car covers 240 km using 20 liters of fuel. What is its mileage in km/liter?", "10", "12", "14", "16", 1),
            Q(AssessmentCategory.Aptitude, "Which number is the odd one out: 3, 5, 7, 9, 11, 15?", "9", "11", "15", "None, all are odd numbers with no other pattern", 3),
            Q(AssessmentCategory.Aptitude, "If the ratio of boys to girls in a class is 3:2 and there are 30 students total, how many boys are there?", "12", "15", "18", "20", 2),
            Q(AssessmentCategory.Aptitude, "A sum of ₹1000 grows to ₹1100 in one year at simple interest. What is the annual interest rate?", "5%", "8%", "10%", "12%", 2),
            Q(AssessmentCategory.Aptitude, "What is the next number in the pattern: 1, 1, 2, 3, 5, 8, ?", "11", "12", "13", "10", 2),
            Q(AssessmentCategory.Aptitude, "If a rectangle has a length of 8 cm and a width of 5 cm, what is its area?", "13 cm²", "26 cm²", "40 cm²", "45 cm²", 2),
            Q(AssessmentCategory.Aptitude, "Three friends split a ₹900 bill equally. How much does each pay?", "₹250", "₹300", "₹350", "₹450", 1),
        });

        return questions;
    }
}
