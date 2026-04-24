using hospital_appointment_management.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace hospital_appointment_management.Areas.Identity.Data;

public class hospital_appointment_managementContext : IdentityDbContext<ApplicationUser>
{
    public hospital_appointment_managementContext(DbContextOptions<hospital_appointment_managementContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new ApplicationUserEntityConfiguration());
        builder.Entity<Appointment>()
        .HasOne(a => a.Patient)
        .WithMany(u => u.PatientAppointments)
        .HasForeignKey(a => a.PatientId)
        .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade to Restrict

        builder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany(u => u.DoctorAppointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict); // Changed from Cascade to Restrict

      
        // Link Doctor to their Feedbacks
        builder.Entity<Feedback>()
            .HasOne(f => f.Doctor)
            .WithMany(u => u.ReceivedFeedbacks)
            .HasForeignKey(f => f.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Link Patient to their written Feedbacks
        builder.Entity<Feedback>()
            .HasOne(f => f.Patient)
            .WithMany() // Or u.WrittenFeedbacks if you add that collection
            .HasForeignKey(f => f.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
    public DbSet<DoctorBlockout> DoctorBlockouts { get; set; }
    public DbSet<DoctorProfile> DoctorProfiles { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
}

internal class ApplicationUserEntityConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FirstName).HasMaxLength(200);
        builder.Property(x => x.LastName).HasMaxLength(200);
       // throw new NotImplementedException();
    }
}