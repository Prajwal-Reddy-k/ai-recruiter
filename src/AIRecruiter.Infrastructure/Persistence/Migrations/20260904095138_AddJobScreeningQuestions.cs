using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruiter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobScreeningQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobScreeningQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobPostingId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    HelpText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PreferredAnswer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobScreeningQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobScreeningQuestions_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScreeningAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobApplicationId = table.Column<int>(type: "int", nullable: false),
                    JobScreeningQuestionId = table.Column<int>(type: "int", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreeningAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScreeningAnswers_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScreeningAnswers_JobScreeningQuestions_JobScreeningQuestionId",
                        column: x => x.JobScreeningQuestionId,
                        principalTable: "JobScreeningQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScreeningQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobScreeningQuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreeningQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScreeningQuestionOptions_JobScreeningQuestions_JobScreeningQuestionId",
                        column: x => x.JobScreeningQuestionId,
                        principalTable: "JobScreeningQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScreeningAnswerSelectedOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScreeningAnswerId = table.Column<int>(type: "int", nullable: false),
                    ScreeningQuestionOptionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreeningAnswerSelectedOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScreeningAnswerSelectedOptions_ScreeningAnswers_ScreeningAnswerId",
                        column: x => x.ScreeningAnswerId,
                        principalTable: "ScreeningAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScreeningAnswerSelectedOptions_ScreeningQuestionOptions_ScreeningQuestionOptionId",
                        column: x => x.ScreeningQuestionOptionId,
                        principalTable: "ScreeningQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobScreeningQuestions_JobPostingId",
                table: "JobScreeningQuestions",
                column: "JobPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningAnswers_JobApplicationId_JobScreeningQuestionId",
                table: "ScreeningAnswers",
                columns: new[] { "JobApplicationId", "JobScreeningQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningAnswers_JobScreeningQuestionId",
                table: "ScreeningAnswers",
                column: "JobScreeningQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningAnswerSelectedOptions_ScreeningAnswerId",
                table: "ScreeningAnswerSelectedOptions",
                column: "ScreeningAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningAnswerSelectedOptions_ScreeningQuestionOptionId",
                table: "ScreeningAnswerSelectedOptions",
                column: "ScreeningQuestionOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScreeningQuestionOptions_JobScreeningQuestionId",
                table: "ScreeningQuestionOptions",
                column: "JobScreeningQuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScreeningAnswerSelectedOptions");

            migrationBuilder.DropTable(
                name: "ScreeningAnswers");

            migrationBuilder.DropTable(
                name: "ScreeningQuestionOptions");

            migrationBuilder.DropTable(
                name: "JobScreeningQuestions");
        }
    }
}
