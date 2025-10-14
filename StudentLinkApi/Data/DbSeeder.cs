using Microsoft.EntityFrameworkCore;
using StudentLinkApi.Data;
using StudentLinkApi.Models;
using BCrypt.Net;

namespace StudentLinkApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Ensure database exists
        await context.Database.EnsureCreatedAsync();

        // Helper local functions
        async Task<User> EnsureUserAsync(string email, string password, string role, string first, string last, string? phone = null, DateTime? createdAt = null)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = role,
                    FirstName = first,
                    LastName = last,
                    PhoneNumber = phone,
                    IsActive = true,
                    CreatedAt = createdAt ?? DateTime.UtcNow
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }
            return user;
        }

        async Task EnsureProfileAsync(User student, string? summary, string? skills, string? education, string? experience)
        {
            var profile = await context.Profiles.FirstOrDefaultAsync(p => p.UserId == student.Id);
            if (profile == null)
            {
                profile = new Profile
                {
                    UserId = student.Id,
                    Summary = summary,
                    Skills = skills,
                    Education = education,
                    Experience = experience,
                    CreatedAt = DateTime.UtcNow
                };
                context.Profiles.Add(profile);
                await context.SaveChangesAsync();
            }
        }

        async Task<Job> EnsureJobAsync(User recruiter, string title, string description, string skills, string location, string jobType, decimal? salaryMin, decimal? salaryMax, int? expYears, string? educationLevel, DateTime? createdAt = null)
        {
            var job = await context.Jobs.FirstOrDefaultAsync(j => j.Title == title && j.RecruiterId == recruiter.Id);
            if (job == null)
            {
                job = new Job
                {
                    RecruiterId = recruiter.Id,
                    Title = title,
                    Description = description,
                    RequiredSkills = skills,
                    Location = location,
                    JobType = jobType,
                    SalaryMin = salaryMin,
                    SalaryMax = salaryMax,
                    ExperienceYears = expYears,
                    EducationLevel = educationLevel,
                    IsActive = true,
                    CreatedAt = createdAt ?? DateTime.UtcNow
                };
                context.Jobs.Add(job);
                await context.SaveChangesAsync();
            }
            return job;
        }

        async Task EnsureApplicationAsync(Guid jobId, Guid userId, string status, string? notes, DateTime? appliedAt = null, DateTime? updatedAt = null)
        {
            var exists = await context.JobApplications.AnyAsync(a => a.JobId == jobId && a.UserId == userId);
            if (!exists)
            {
                var app = new JobApplication
                {
                    JobId = jobId,
                    UserId = userId,
                    Status = status,
                    Notes = notes,
                    AppliedAt = appliedAt ?? DateTime.UtcNow,
                    UpdatedAt = updatedAt
                };
                context.JobApplications.Add(app);
                await context.SaveChangesAsync();
            }
        }

        // Create core users
        var admin = await EnsureUserAsync("admin@studentlink.com", "Admin123!", "Admin", "System", "Administrator", "+1-555-0100", DateTime.UtcNow.AddDays(-90));
        var recruiter1 = await EnsureUserAsync("recruiter1@techcorp.com", "Recruiter123!", "Recruiter", "Sarah", "Johnson", "+1-555-0201", DateTime.UtcNow.AddDays(-60));
        var recruiter2 = await EnsureUserAsync("recruiter2@innovate.com", "Recruiter123!", "Recruiter", "Michael", "Chen", "+1-555-0202", DateTime.UtcNow.AddDays(-55));

        var student1 = await EnsureUserAsync("john.doe@student.com", "Student123!", "Student", "John", "Doe", "+1-555-1001", DateTime.UtcNow.AddDays(-50));
        var student2 = await EnsureUserAsync("jane.smith@student.com", "Student123!", "Student", "Jane", "Smith", "+1-555-1002", DateTime.UtcNow.AddDays(-48));
        var student3 = await EnsureUserAsync("alex.wong@student.com", "Student123!", "Student", "Alex", "Wong", "+1-555-1003", DateTime.UtcNow.AddDays(-46));
        var student4 = await EnsureUserAsync("emily.brown@student.com", "Student123!", "Student", "Emily", "Brown", "+1-555-1004", DateTime.UtcNow.AddDays(-44));
        var student5 = await EnsureUserAsync("david.lee@student.com", "Student123!", "Student", "David", "Lee", "+1-555-1005", DateTime.UtcNow.AddDays(-42));

        // Profiles
        await EnsureProfileAsync(student1,
            "Passionate Computer Science student with strong programming skills and experience in web development.",
            "C#, JavaScript, React, SQL, Azure, Git",
            "[{\"degree\":\"Bachelor of Computer Science\",\"school\":\"Tech University\",\"year\":\"2024\"}]",
            "[{\"title\":\"Junior Developer Intern\",\"company\":\"StartupXYZ\",\"duration\":\"3 months\"}]");
        await EnsureProfileAsync(student2,
            "Data Science enthusiast with expertise in machine learning and statistical analysis.",
            "Python, R, TensorFlow, Pandas, SQL, Machine Learning",
            "[{\"degree\":\"Bachelor of Data Science\",\"school\":\"Data University\",\"year\":\"2024\"}]",
            "[{\"title\":\"Data Analyst Intern\",\"company\":\"Analytics Inc\",\"duration\":\"6 months\"}]");
        await EnsureProfileAsync(student3,
            "Full-stack developer with a passion for creating intuitive user experiences.",
            "JavaScript, TypeScript, Node.js, React, MongoDB, Docker",
            "[{\"degree\":\"Bachelor of Software Engineering\",\"school\":\"Code Academy\",\"year\":\"2024\"}]",
            "[{\"title\":\"Web Developer\",\"company\":\"WebCo\",\"duration\":\"4 months\"}]");
        await EnsureProfileAsync(student4,
            "Cybersecurity specialist focused on network security and ethical hacking.",
            "Network Security, Penetration Testing, Linux, Python, Wireshark",
            "[{\"degree\":\"Bachelor of Cybersecurity\",\"school\":\"Security College\",\"year\":\"2024\"}]",
            "[{\"title\":\"Security Intern\",\"company\":\"SecureNet\",\"duration\":\"2 months\"}]");
        await EnsureProfileAsync(student5,
            "Mobile app developer specializing in cross-platform development.",
            "Flutter, Dart, React Native, Firebase, iOS, Android",
            "[{\"degree\":\"Bachelor of Mobile Development\",\"school\":\"App University\",\"year\":\"2024\"}]",
            "[{\"title\":\"Mobile Dev Intern\",\"company\":\"MobileFirst\",\"duration\":\"5 months\"}]");

        // Jobs
        var job1 = await EnsureJobAsync(recruiter1, "Junior Software Developer",
            "Looking for a passionate junior developer to join our growing team. You will work on exciting projects using modern technologies.",
            "C#, .NET, SQL, Git", "New York, NY", "Full-time", 60000, 80000, 0, "Bachelor's Degree", DateTime.UtcNow.AddDays(-20));
        var job2 = await EnsureJobAsync(recruiter1, "Frontend Developer Intern",
            "Internship opportunity for students interested in modern web development. Learn React and TypeScript while building real applications.",
            "JavaScript, React, HTML, CSS", "San Francisco, CA", "Internship", 25, 35, 0, "Currently enrolled", DateTime.UtcNow.AddDays(-15));
        var job3 = await EnsureJobAsync(recruiter2, "Data Analyst",
            "Join our data team to help drive business decisions through data analysis and visualization.",
            "Python, SQL, Tableau, Excel", "Austin, TX", "Full-time", 65000, 85000, 1, "Bachelor's Degree", DateTime.UtcNow.AddDays(-12));
        var job4 = await EnsureJobAsync(recruiter2, "Mobile App Developer",
            "Build cutting-edge mobile applications for iOS and Android. Experience with Flutter is a plus.",
            "Flutter, Dart, Mobile Development", "Seattle, WA", "Full-time", 70000, 95000, 1, "Bachelor's Degree", DateTime.UtcNow.AddDays(-8));
        var job5 = await EnsureJobAsync(recruiter1, "DevOps Engineer",
            "Help us build and maintain our cloud infrastructure. Experience with Azure and Docker required.",
            "Azure, Docker, Kubernetes, CI/CD", "Remote", "Full-time", 80000, 110000, 2, "Bachelor's Degree", DateTime.UtcNow.AddDays(-5));

        // Applications
        await EnsureApplicationAsync(job1.Id, student1.Id, "Applied", "Very interested in this position!", DateTime.UtcNow.AddDays(-10));
        await EnsureApplicationAsync(job2.Id, student1.Id, "Interview", "Looking forward to the internship", DateTime.UtcNow.AddDays(-8), DateTime.UtcNow.AddDays(-3));
        await EnsureApplicationAsync(job3.Id, student2.Id, "Applied", "Perfect match for my data science background", DateTime.UtcNow.AddDays(-7));
        await EnsureApplicationAsync(job4.Id, student5.Id, "Reviewed", "Experienced with Flutter", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-2));
        await EnsureApplicationAsync(job1.Id, student3.Id, "Offer", "Excited about this opportunity!", DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(-1));
    }
}
