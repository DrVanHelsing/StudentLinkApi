using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentLinkApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractiveFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_CVs_UploadedAt",
                table: "CVs");

            migrationBuilder.AlterColumn<decimal>(
                name: "MatchScore",
                table: "JobMatches",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "QualityScore",
                table: "CVFeedbacks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AIConfidenceScore",
                table: "CVAnalysisResults",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "CVImprovementProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalUploads = table.Column<int>(type: "int", nullable: false),
                    InitialScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CurrentScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ImprovementPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CompletedActions = table.Column<int>(type: "int", nullable: false),
                    TotalActions = table.Column<int>(type: "int", nullable: false),
                    FirstUploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CVImprovementProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVImprovementProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CVInteractiveFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CVId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ContactSectionFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactSectionScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SummarySectionFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SummarySectionScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ExperienceSectionFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExperienceSectionScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EducationSectionFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EducationSectionScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SkillsSectionFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SkillsSectionScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ImprovementPriorities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextSteps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImprovementFromPrevious = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CVInteractiveFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVInteractiveFeedbacks_CVs_CVId",
                        column: x => x.CVId,
                        principalTable: "CVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CVInteractiveFeedbacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CVImprovementProgresses_UserId",
                table: "CVImprovementProgresses",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CVInteractiveFeedbacks_CVId",
                table: "CVInteractiveFeedbacks",
                column: "CVId");

            migrationBuilder.CreateIndex(
                name: "IX_CVInteractiveFeedbacks_UserId",
                table: "CVInteractiveFeedbacks",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobMatches_Jobs_JobId",
                table: "JobMatches",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobMatches_Jobs_JobId",
                table: "JobMatches");

            migrationBuilder.DropTable(
                name: "CVImprovementProgresses");

            migrationBuilder.DropTable(
                name: "CVInteractiveFeedbacks");

            migrationBuilder.AlterColumn<decimal>(
                name: "MatchScore",
                table: "JobMatches",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "QualityScore",
                table: "CVFeedbacks",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "AIConfidenceScore",
                table: "CVAnalysisResults",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_CVs_UploadedAt",
                table: "CVs",
                column: "UploadedAt");
        }
    }
}
