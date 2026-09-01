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
        if (alreadySeeded)
        {
            return;
        }

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
}
