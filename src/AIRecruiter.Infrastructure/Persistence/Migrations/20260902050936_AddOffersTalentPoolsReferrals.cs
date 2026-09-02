using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruiter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOffersTalentPoolsReferrals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobApplicationId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    OfferedSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalaryType = table.Column<int>(type: "int", nullable: false),
                    JoiningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkCity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRemote = table.Column<bool>(type: "bit", nullable: false),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    ProbationDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Benefits = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecruiterMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CandidateResponseNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferrerUserId = table.Column<int>(type: "int", nullable: false),
                    JobPostingId = table.Column<int>(type: "int", nullable: false),
                    ReferredName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferredEmail = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReferredPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelevantSkillsCsv = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TokenExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RegisteredUserId = table.Column<int>(type: "int", nullable: true),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JobApplicationId = table.Column<int>(type: "int", nullable: true),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Referrals_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ReferrerUserId",
                        column: x => x.ReferrerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_RegisteredUserId",
                        column: x => x.RegisteredUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TalentPools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentPools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalentPools_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TalentPools_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfferStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfferId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: true),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferStatusHistories_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OfferStatusHistories_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TalentPoolCandidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TalentPoolId = table.Column<int>(type: "int", nullable: false),
                    CandidateProfileId = table.Column<int>(type: "int", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TagsCsv = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentPoolCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalentPoolCandidates_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TalentPoolCandidates_TalentPools_TalentPoolId",
                        column: x => x.TalentPoolId,
                        principalTable: "TalentPools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TalentPoolCandidates_Users_AddedByUserId",
                        column: x => x.AddedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CreatedByUserId",
                table: "Offers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_JobApplicationId_Status",
                table: "Offers",
                columns: new[] { "JobApplicationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OfferStatusHistories_ChangedByUserId",
                table: "OfferStatusHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferStatusHistories_OfferId",
                table: "OfferStatusHistories",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_JobApplicationId",
                table: "Referrals",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_JobPostingId",
                table: "Referrals",
                column: "JobPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredEmail_JobPostingId",
                table: "Referrals",
                columns: new[] { "ReferredEmail", "JobPostingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerUserId",
                table: "Referrals",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_RegisteredUserId",
                table: "Referrals",
                column: "RegisteredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_TokenHash",
                table: "Referrals",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolCandidates_AddedByUserId",
                table: "TalentPoolCandidates",
                column: "AddedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolCandidates_CandidateProfileId",
                table: "TalentPoolCandidates",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentPoolCandidates_TalentPoolId_CandidateProfileId",
                table: "TalentPoolCandidates",
                columns: new[] { "TalentPoolId", "CandidateProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TalentPools_CompanyId",
                table: "TalentPools",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentPools_CreatedByUserId",
                table: "TalentPools",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferStatusHistories");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "TalentPoolCandidates");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "TalentPools");
        }
    }
}
