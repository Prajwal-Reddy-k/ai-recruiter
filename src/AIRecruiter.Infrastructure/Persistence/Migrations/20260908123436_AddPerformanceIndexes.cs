using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruiter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Messages_JobApplicationId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_CompanyId",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_RecruiterProfileId",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_CandidateProfileId",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_Interviews_JobApplicationId",
                table: "Interviews");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_ActorUserId",
                table: "AuditLogEntries");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_JobApplicationId_CreatedAt",
                table: "Messages",
                columns: new[] { "JobApplicationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_CompanyId_Status_ModerationStatus_CreatedAt",
                table: "JobPostings",
                columns: new[] { "CompanyId", "Status", "ModerationStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_RecruiterProfileId_CreatedAt",
                table: "JobPostings",
                columns: new[] { "RecruiterProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_Status_ModerationStatus_CreatedAt",
                table: "JobPostings",
                columns: new[] { "Status", "ModerationStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_CandidateProfileId_CreatedAt",
                table: "JobApplications",
                columns: new[] { "CandidateProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobPostingId_CreatedAt",
                table: "JobApplications",
                columns: new[] { "JobPostingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_JobApplicationId_CreatedAt",
                table: "Interviews",
                columns: new[] { "JobApplicationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_Status_ScheduledStartUtc",
                table: "Interviews",
                columns: new[] { "Status", "ScheduledStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ActorUserId_TimestampUtc",
                table: "AuditLogEntries",
                columns: new[] { "ActorUserId", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Messages_JobApplicationId_CreatedAt",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_CompanyId_Status_ModerationStatus_CreatedAt",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_RecruiterProfileId_CreatedAt",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_Status_ModerationStatus_CreatedAt",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_CandidateProfileId_CreatedAt",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_JobPostingId_CreatedAt",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_Interviews_JobApplicationId_CreatedAt",
                table: "Interviews");

            migrationBuilder.DropIndex(
                name: "IX_Interviews_Status_ScheduledStartUtc",
                table: "Interviews");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogEntries_ActorUserId_TimestampUtc",
                table: "AuditLogEntries");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_JobApplicationId",
                table: "Messages",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_CompanyId",
                table: "JobPostings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_RecruiterProfileId",
                table: "JobPostings",
                column: "RecruiterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_CandidateProfileId",
                table: "JobApplications",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_JobApplicationId",
                table: "Interviews",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_ActorUserId",
                table: "AuditLogEntries",
                column: "ActorUserId");
        }
    }
}
