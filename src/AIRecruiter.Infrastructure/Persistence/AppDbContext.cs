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
    public DbSet<InterviewSlot> InterviewSlots => Set<InterviewSlot>();
    public DbSet<SavedJob> SavedJobs => Set<SavedJob>();
    public DbSet<JobAlert> JobAlerts => Set<JobAlert>();
    public DbSet<JobReport> JobReports => Set<JobReport>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

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

        modelBuilder.Entity<InterviewSlot>()
            .HasOne(s => s.Interview)
            .WithMany(i => i.Slots)
            .HasForeignKey(s => s.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // --- Job reports ---
        modelBuilder.Entity<JobReport>()
            .HasOne(r => r.JobPosting)
            .WithMany(j => j.Reports)
            .HasForeignKey(r => r.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobReport>()
            .HasOne(r => r.ReportedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobReport>()
            .HasOne(r => r.ReviewedByUser)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Audit log ---
        modelBuilder.Entity<AuditLogEntry>()
            .HasOne(e => e.ActorUser)
            .WithMany()
            .HasForeignKey(e => e.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

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
