using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruiter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationSuspensionPreferencesInvitationsJobExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobReports");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApplicationDeadlineUtc",
                table: "JobPostings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvailabilityStatus",
                table: "CandidateProfiles",
                type: "int",
                nullable: false,
                // AvailabilityStatus has no zero member — entity default is
                // OpenToOpportunities (2), so existing candidates backfill to that.
                defaultValue: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedSalaryMax",
                table: "CandidateProfiles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedSalaryMin",
                table: "CandidateProfiles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoticePeriodDays",
                table: "CandidateProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredJobTypesCsv",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLocationsCsv",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredRolesCsv",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileVisibility",
                table: "CandidateProfiles",
                type: "int",
                nullable: false,
                // ProfileVisibility has no zero member — entity default is
                // VisibleAfterApplying (2), exactly matching today's actual behavior, so no
                // existing candidate becomes newly exposed or newly hidden by this migration.
                defaultValue: 2);

            migrationBuilder.AddColumn<bool>(
                name: "RemotePreference",
                table: "CandidateProfiles",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobPostingId = table.Column<int>(type: "int", nullable: false),
                    CandidateProfileId = table.Column<int>(type: "int", nullable: false),
                    InvitedByUserId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitations_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invitations_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invitations_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    ReportedByUserId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModerationNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_CandidateProfileId",
                table: "Invitations",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_InvitedByUserId",
                table: "Invitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_JobPostingId_CandidateProfileId_Status",
                table: "Invitations",
                columns: new[] { "JobPostingId", "CandidateProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_EntityType_EntityId",
                table: "Reports",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedByUserId",
                table: "Reports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReviewedByUserId",
                table: "Reports",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropColumn(
                name: "ApplicationDeadlineUtc",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ExpectedSalaryMax",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ExpectedSalaryMin",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "NoticePeriodDays",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredJobTypesCsv",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredLocationsCsv",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredRolesCsv",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileVisibility",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "RemotePreference",
                table: "CandidateProfiles");

            migrationBuilder.CreateTable(
                name: "JobReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobPostingId = table.Column<int>(type: "int", nullable: false),
                    ReportedByUserId = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobReports_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobReports_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobReports_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobReports_JobPostingId",
                table: "JobReports",
                column: "JobPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_JobReports_ReportedByUserId",
                table: "JobReports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobReports_ReviewedByUserId",
                table: "JobReports",
                column: "ReviewedByUserId");
        }
    }
}
