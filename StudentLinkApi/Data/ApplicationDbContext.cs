using Microsoft.EntityFrameworkCore;
using StudentLinkApi.Models;

namespace StudentLinkApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<CV> CVs { get; set; }
    public DbSet<CVFeedback> CVFeedbacks { get; set; }
    public DbSet<CVAnalysisResult> CVAnalysisResults { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobMatch> JobMatches { get; set; }
    public DbSet<CVInteractiveFeedback> CVInteractiveFeedbacks { get; set; }
    public DbSet<CVImprovementProgress> CVImprovementProgresses { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Profile configuration
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<Profile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // CV configuration
        modelBuilder.Entity<CV>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.CVs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FileType).HasMaxLength(50);
            entity.Property(e => e.FileUrl).HasMaxLength(500);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // CVFeedback configuration
        modelBuilder.Entity<CVFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CV)
                .WithMany(c => c.Feedbacks)
                .HasForeignKey(e => e.CVId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(e => e.QualityScore).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.CVId);
            entity.HasIndex(e => e.UserId);
        });

        // CVAnalysisResult configuration
        modelBuilder.Entity<CVAnalysisResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CV)
                .WithMany(c => c.AnalysisResults)
                .HasForeignKey(e => e.CVId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.AIConfidenceScore).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.CVId);
        });

        // Job configuration
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Recruiter)
                .WithMany()
                .HasForeignKey(e => e.RecruiterId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.RequiredSkills).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.JobType).HasMaxLength(50);
            entity.Property(e => e.SalaryMin).HasPrecision(18, 2);
            entity.Property(e => e.SalaryMax).HasPrecision(18, 2);
            entity.Property(e => e.EducationLevel).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.RecruiterId);
        });

        // JobMatch configuration
        modelBuilder.Entity<JobMatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(e => e.MatchScore).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => new { e.UserId, e.JobId }).IsUnique();
        });

        // CVInteractiveFeedback configuration
        modelBuilder.Entity<CVInteractiveFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.CV)
                .WithMany()
                .HasForeignKey(e => e.CVId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.Property(e => e.OverallScore).HasPrecision(5, 2);
            entity.Property(e => e.ContactSectionScore).HasPrecision(5, 2);
            entity.Property(e => e.SummarySectionScore).HasPrecision(5, 2);
            entity.Property(e => e.ExperienceSectionScore).HasPrecision(5, 2);
            entity.Property(e => e.EducationSectionScore).HasPrecision(5, 2);
            entity.Property(e => e.SkillsSectionScore).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => e.CVId);
            entity.HasIndex(e => e.UserId);
        });

        // CVImprovementProgress configuration
        modelBuilder.Entity<CVImprovementProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.InitialScore).HasPrecision(5, 2);
            entity.Property(e => e.CurrentScore).HasPrecision(5, 2);
            entity.Property(e => e.ImprovementPercentage).HasPrecision(5, 2);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // JobApplication configuration
        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Job)
                .WithMany()
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
            entity.Property(e => e.AppliedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(e => new { e.UserId, e.JobId }).IsUnique();
        });
    }
}
