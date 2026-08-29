using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruiter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingProfilesApplicationsResumeMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResumeUrl",
                table: "CandidateProfiles",
                newName: "ResumeStorageKey");

            migrationBuilder.AddColumn<string>(
                name: "MatchedSkillsCsv",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingSkillsCsv",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoringExplanation",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedImprovements",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Education",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperienceSummary",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LocationLat",
                table: "CandidateProfiles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LocationLng",
                table: "CandidateProfiles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeContentType",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeExtractedText",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeOriginalFileName",
                table: "CandidateProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ResumeSizeBytes",
                table: "CandidateProfiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumeUploadedAt",
                table: "CandidateProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchedSkillsCsv",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "MissingSkillsCsv",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ScoringExplanation",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "SuggestedImprovements",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "Education",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ExperienceSummary",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "LocationLat",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "LocationLng",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeContentType",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeExtractedText",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeOriginalFileName",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeSizeBytes",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeUploadedAt",
                table: "CandidateProfiles");

            migrationBuilder.RenameColumn(
                name: "ResumeStorageKey",
                table: "CandidateProfiles",
                newName: "ResumeUrl");
        }
    }
}
