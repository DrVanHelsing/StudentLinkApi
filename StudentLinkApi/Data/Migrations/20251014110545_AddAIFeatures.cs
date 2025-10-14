using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentLinkApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAIFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CVAnalysisResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CVId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractedSkills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractedExperience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractedEducation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractedContact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AIConfidenceScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CVAnalysisResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVAnalysisResults_CVs_CVId",
                        column: x => x.CVId,
                        principalTable: "CVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CVFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CVId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeedbackText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QualityScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StructureIssues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrammarIssues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MissingFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CVFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVFeedbacks_CVs_CVId",
                        column: x => x.CVId,
                        principalTable: "CVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CVFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MatchReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsViewed = table.Column<bool>(type: "bit", nullable: false),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobMatches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecruiterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredSkills = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JobType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SalaryMin = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SalaryMax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExperienceYears = table.Column<int>(type: "int", nullable: true),
                    EducationLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_Users_RecruiterId",
                        column: x => x.RecruiterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CVAnalysisResults_CVId",
                table: "CVAnalysisResults",
                column: "CVId");

            migrationBuilder.CreateIndex(
                name: "IX_CVFeedbacks_CVId",
                table: "CVFeedbacks",
                column: "CVId");

            migrationBuilder.CreateIndex(
                name: "IX_CVFeedbacks_UserId",
                table: "CVFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobMatches_JobId",
                table: "JobMatches",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobMatches_UserId",
                table: "JobMatches",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobMatches_UserId_JobId",
                table: "JobMatches",
                columns: new[] { "UserId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IsActive",
                table: "Jobs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_RecruiterId",
                table: "Jobs",
                column: "RecruiterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CVAnalysisResults");

            migrationBuilder.DropTable(
                name: "CVFeedbacks");

            migrationBuilder.DropTable(
                name: "JobMatches");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
