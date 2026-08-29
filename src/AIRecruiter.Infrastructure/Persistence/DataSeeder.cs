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

        var recruiter1 = new RecruiterProfile { User = recruiter1User, Company = nimbus, Designation = "Talent Acquisition Lead" };
        var recruiter2 = new RecruiterProfile { User = recruiter2User, Company = bluepeak, Designation = "HR Manager" };
        var recruiter3 = new RecruiterProfile { User = recruiter3User, Company = solstice, Designation = "Recruiter" };
        var recruiter4 = new RecruiterProfile { User = recruiter4User, Company = vertex, Designation = "Talent Partner" };
        var recruiter5 = new RecruiterProfile { User = recruiter5User, Company = northgate, Designation = "Recruiter" };
        var recruiter6 = new RecruiterProfile { User = recruiter6User, Company = coral, Designation = "People Operations" };

        db.RecruiterProfiles.AddRange(recruiter1, recruiter2, recruiter3, recruiter4, recruiter5, recruiter6);

        // --- Candidates -------------------------------------------------------------
        var candidate1User = new User { FullName = "Arjun Mehta", Email = $"candidate1{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate2User = new User { FullName = "Sara Kapoor", Email = $"candidate2{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate3User = new User { FullName = "Liam Fernandes", Email = $"candidate3{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate4User = new User { FullName = "Fatima Sheikh", Email = $"candidate4{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate5User = new User { FullName = "Rohan Gupta", Email = $"candidate5{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate6User = new User { FullName = "Divya Krishnan", Email = $"candidate6{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate7User = new User { FullName = "Aditi Shah", Email = $"candidate7{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };
        var candidate8User = new User { FullName = "Vikram Singh", Email = $"candidate8{EmailDomain}", PasswordHash = passwordHash, Role = UserRole.Candidate };

        var candidate1 = new CandidateProfile { User = candidate1User, Headline = "Senior Backend Engineer", Summary = "Backend engineer focused on .NET services and cloud-native APIs.", Education = "B.Tech in Computer Science, IIT Bombay", ExperienceSummary = "4 years building and operating ASP.NET Core services at scale.", TotalExperienceYears = 4, City = "Bengaluru", State = "Karnataka", SkillsCsv = "C#, ASP.NET Core, SQL Server, Docker, Entity Framework Core" };
        var candidate2 = new CandidateProfile { User = candidate2User, Headline = "Data Analyst", Summary = "Data analyst turning messy data into decisions.", Education = "B.Sc. Statistics, University of Hyderabad", ExperienceSummary = "2 years in retail analytics and dashboarding.", TotalExperienceYears = 2, City = "Hyderabad", State = "Telangana", SkillsCsv = "Python, SQL, Power BI, Pandas, Data Analysis" };
        var candidate3 = new CandidateProfile { User = candidate3User, Headline = "Frontend Developer", Summary = "Frontend developer specializing in React and design systems.", Education = "B.E. Computer Engineering, College of Engineering Pune", ExperienceSummary = "3 years shipping consumer-facing React applications.", TotalExperienceYears = 3, City = "Pune", State = "Maharashtra", SkillsCsv = "React, TypeScript, CSS, HTML, UI/UX" };
        var candidate4 = new CandidateProfile { User = candidate4User, Headline = "DevOps Engineer", Summary = "DevOps engineer automating infrastructure on AWS.", Education = "B.Tech Computer Engineering, VJTI Mumbai", ExperienceSummary = "5 years running Kubernetes clusters and CI/CD pipelines.", TotalExperienceYears = 5, City = "Mumbai", State = "Maharashtra", SkillsCsv = "AWS, Docker, Kubernetes, CI/CD, Terraform" };
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

        // --- Applications ----------------------------------------------------------------
        db.JobApplications.AddRange(
            new JobApplication { JobPosting = jobSeniorBackend, CandidateProfile = candidate1, Status = ApplicationStatus.Shortlisted },
            new JobApplication { JobPosting = jobCloudDevOps, CandidateProfile = candidate1, Status = ApplicationStatus.Applied },
            new JobApplication { JobPosting = jobDataAnalyst, CandidateProfile = candidate2, Status = ApplicationStatus.InterviewScheduled },
            new JobApplication { JobPosting = jobMlEngineer, CandidateProfile = candidate2, Status = ApplicationStatus.Screening },
            new JobApplication { JobPosting = jobFrontendReact, CandidateProfile = candidate3, Status = ApplicationStatus.Hired },
            new JobApplication { JobPosting = jobUiUxDesigner, CandidateProfile = candidate3, Status = ApplicationStatus.Rejected },
            new JobApplication { JobPosting = jobBackendPayments, CandidateProfile = candidate4, Status = ApplicationStatus.Shortlisted },
            new JobApplication { JobPosting = jobQaMumbai, CandidateProfile = candidate5, Status = ApplicationStatus.Applied },
            new JobApplication { JobPosting = jobFrontendDelhi, CandidateProfile = candidate5, Status = ApplicationStatus.Screening },
            new JobApplication { JobPosting = jobFullStackChennai, CandidateProfile = candidate6, Status = ApplicationStatus.Offer },
            new JobApplication { JobPosting = jobSupportChennai, CandidateProfile = candidate6, Status = ApplicationStatus.Applied },
            new JobApplication { JobPosting = jobUiUxDesigner, CandidateProfile = candidate7, Status = ApplicationStatus.InterviewCompleted },
            new JobApplication { JobPosting = jobBackendHealthKochi, CandidateProfile = candidate8, Status = ApplicationStatus.Applied },
            new JobApplication { JobPosting = jobSeniorBackend, CandidateProfile = candidate6, Status = ApplicationStatus.Withdrawn },
            new JobApplication { JobPosting = jobDataAnalyst, CandidateProfile = candidate1, Status = ApplicationStatus.Rejected });

        await db.SaveChangesAsync(ct);

        // A pending report so the admin review queue isn't empty.
        db.JobReports.Add(new JobReport
        {
            JobPostingId = jobSupportKolkata.Id,
            ReportedByUserId = candidate1User.Id,
            Reason = "Listing appears stale — role was filled weeks ago.",
        });

        await db.SaveChangesAsync(ct);
    }
}
