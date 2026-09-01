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
