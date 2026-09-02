using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruiter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverLettersAssessmentsPublicProfileCareerGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicProfileSlug",
                table: "CandidateProfiles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CareerGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateProfileId = table.Column<int>(type: "int", nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetSkill = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetCompanyType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsLocationRemote = table.Column<bool>(type: "bit", nullable: false),
                    TargetCompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerGoals_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoverLetterTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateProfileId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Introduction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SkillsHighlights = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectAchievements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosingMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoverLetterTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoverLetterTemplates_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillAssessmentAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateProfileId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScoreCorrectCount = table.Column<int>(type: "int", nullable: true),
                    TotalQuestionCount = table.Column<int>(type: "int", nullable: true),
                    PercentageScore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsVisibleToRecruiters = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillAssessmentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillAssessmentAttempts_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillAssessmentQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionB = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionD = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectOptionIndex = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillAssessmentQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillAssessmentAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillAssessmentAttemptId = table.Column<int>(type: "int", nullable: false),
                    SkillAssessmentQuestionId = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    SelectedOptionIndex = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillAssessmentAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillAssessmentAnswers_SkillAssessmentAttempts_SkillAssessmentAttemptId",
                        column: x => x.SkillAssessmentAttemptId,
                        principalTable: "SkillAssessmentAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkillAssessmentAnswers_SkillAssessmentQuestions_SkillAssessmentQuestionId",
                        column: x => x.SkillAssessmentQuestionId,
                        principalTable: "SkillAssessmentQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_PublicProfileSlug",
                table: "CandidateProfiles",
                column: "PublicProfileSlug",
                unique: true,
                filter: "[PublicProfileSlug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CareerGoals_CandidateProfileId",
                table: "CareerGoals",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CoverLetterTemplates_CandidateProfileId",
                table: "CoverLetterTemplates",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssessmentAnswers_SkillAssessmentAttemptId",
                table: "SkillAssessmentAnswers",
                column: "SkillAssessmentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssessmentAnswers_SkillAssessmentQuestionId",
                table: "SkillAssessmentAnswers",
                column: "SkillAssessmentQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssessmentAttempts_CandidateProfileId",
                table: "SkillAssessmentAttempts",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillAssessmentQuestions_Category",
                table: "SkillAssessmentQuestions",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerGoals");

            migrationBuilder.DropTable(
                name: "CoverLetterTemplates");

            migrationBuilder.DropTable(
                name: "SkillAssessmentAnswers");

            migrationBuilder.DropTable(
                name: "SkillAssessmentAttempts");

            migrationBuilder.DropTable(
                name: "SkillAssessmentQuestions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_PublicProfileSlug",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PublicProfileSlug",
                table: "CandidateProfiles");
        }
    }
}
