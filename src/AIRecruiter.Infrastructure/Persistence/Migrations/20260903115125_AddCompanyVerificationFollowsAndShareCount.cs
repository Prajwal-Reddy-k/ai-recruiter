using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruiter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyVerificationFollowsAndShareCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShareCount",
                table: "JobPostings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BusinessEmail",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationDocumentReference",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationNote",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationReviewedAtUtc",
                table: "Companies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationReviewedByUserId",
                table: "Companies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationSubmittedAtUtc",
                table: "Companies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyFollows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateProfileId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    NotifyOnNewJob = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyFollows_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyFollows_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_VerificationReviewedByUserId",
                table: "Companies",
                column: "VerificationReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFollows_CandidateProfileId_CompanyId",
                table: "CompanyFollows",
                columns: new[] { "CandidateProfileId", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFollows_CompanyId",
                table: "CompanyFollows",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Users_VerificationReviewedByUserId",
                table: "Companies",
                column: "VerificationReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Users_VerificationReviewedByUserId",
                table: "Companies");

            migrationBuilder.DropTable(
                name: "CompanyFollows");

            migrationBuilder.DropIndex(
                name: "IX_Companies_VerificationReviewedByUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ShareCount",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "BusinessEmail",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationDocumentReference",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationNote",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationReviewedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationReviewedByUserId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VerificationSubmittedAtUtc",
                table: "Companies");
        }
    }
}
