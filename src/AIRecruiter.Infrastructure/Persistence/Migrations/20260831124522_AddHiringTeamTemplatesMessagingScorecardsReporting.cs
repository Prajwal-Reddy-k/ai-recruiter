using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruiter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHiringTeamTemplatesMessagingScorecardsReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyRole",
                table: "RecruiterProfiles",
                type: "int",
                nullable: false,
                // CompanyRole has no zero member (Owner=1) — every existing recruiter
                // becomes an Owner of their company, matching their pre-existing full
                // access before this concept existed.
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "InterviewAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewId = table.Column<int>(type: "int", nullable: false),
                    RecruiterProfileId = table.Column<int>(type: "int", nullable: false),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewAssignments_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewAssignments_RecruiterProfiles_RecruiterProfileId",
                        column: x => x.RecruiterProfileId,
                        principalTable: "RecruiterProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InterviewAssignments_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterviewFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewId = table.Column<int>(type: "int", nullable: false),
                    RecruiterProfileId = table.Column<int>(type: "int", nullable: false),
                    TechnicalScore = table.Column<int>(type: "int", nullable: false),
                    CommunicationScore = table.Column<int>(type: "int", nullable: false),
                    ProblemSolvingScore = table.Column<int>(type: "int", nullable: false),
                    CultureFitScore = table.Column<int>(type: "int", nullable: false),
                    Recommendation = table.Column<int>(type: "int", nullable: false),
                    Strengths = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Concerns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrivateNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_Interviews_InterviewId",
                        column: x => x.InterviewId,
                        principalTable: "Interviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbacks_RecruiterProfiles_RecruiterProfileId",
                        column: x => x.RecruiterProfileId,
                        principalTable: "RecruiterProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobPostingId = table.Column<int>(type: "int", nullable: false),
                    RecruiterProfileId = table.Column<int>(type: "int", nullable: false),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobAssignments_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobAssignments_RecruiterProfiles_RecruiterProfileId",
                        column: x => x.RecruiterProfileId,
                        principalTable: "RecruiterProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobAssignments_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    RecruiterProfileId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Responsibilities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiredSkillsCsv = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredSkillsCsv = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinExperienceYears = table.Column<int>(type: "int", nullable: true),
                    MaxExperienceYears = table.Column<int>(type: "int", nullable: true),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    SalaryVisible = table.Column<bool>(type: "bit", nullable: false),
                    MinSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DefaultCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultLocality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultIsRemote = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTemplates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobTemplates_RecruiterProfiles_RecruiterProfileId",
                        column: x => x.RecruiterProfileId,
                        principalTable: "RecruiterProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobApplicationId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    SenderRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Users_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterviewFeedbackEditHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewFeedbackId = table.Column<int>(type: "int", nullable: false),
                    EditedByUserId = table.Column<int>(type: "int", nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewFeedbackEditHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbackEditHistories_InterviewFeedbacks_InterviewFeedbackId",
                        column: x => x.InterviewFeedbackId,
                        principalTable: "InterviewFeedbacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewFeedbackEditHistories_Users_EditedByUserId",
                        column: x => x.EditedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssignments_AssignedByUserId",
                table: "InterviewAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssignments_InterviewId_RecruiterProfileId",
                table: "InterviewAssignments",
                columns: new[] { "InterviewId", "RecruiterProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssignments_RecruiterProfileId",
                table: "InterviewAssignments",
                column: "RecruiterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbackEditHistories_EditedByUserId",
                table: "InterviewFeedbackEditHistories",
                column: "EditedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbackEditHistories_InterviewFeedbackId",
                table: "InterviewFeedbackEditHistories",
                column: "InterviewFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_InterviewId_RecruiterProfileId",
                table: "InterviewFeedbacks",
                columns: new[] { "InterviewId", "RecruiterProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewFeedbacks_RecruiterProfileId",
                table: "InterviewFeedbacks",
                column: "RecruiterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_AssignedByUserId",
                table: "JobAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_JobPostingId_RecruiterProfileId",
                table: "JobAssignments",
                columns: new[] { "JobPostingId", "RecruiterProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignments_RecruiterProfileId",
                table: "JobAssignments",
                column: "RecruiterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_CompanyId",
                table: "JobTemplates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTemplates_RecruiterProfileId",
                table: "JobTemplates",
                column: "RecruiterProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_JobApplicationId",
                table: "Messages",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderUserId",
                table: "Messages",
                column: "SenderUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewAssignments");

            migrationBuilder.DropTable(
                name: "InterviewFeedbackEditHistories");

            migrationBuilder.DropTable(
                name: "JobAssignments");

            migrationBuilder.DropTable(
                name: "JobTemplates");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "InterviewFeedbacks");

            migrationBuilder.DropColumn(
                name: "CompanyRole",
                table: "RecruiterProfiles");
        }
    }
}
