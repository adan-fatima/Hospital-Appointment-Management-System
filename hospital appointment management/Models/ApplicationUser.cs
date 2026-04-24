using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
namespace hospital_appointment_management.Models
{
        public class ApplicationUser : IdentityUser
        {
        // This adds the column to the database so we can do doctor.User.FullName
       
            // --- Common Properties ---
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string? Address { get; set; }

        // --- Properties from your old Doctor class ---
        // We make these nullable (?) because Patients won't have them
        public string? LicenseNumber { get; set; }
        public bool IsApproved { get; set; } = false;
        public string? Specialization { get; set; }
            public string? Bio { get; set; }
            public string? ProfilePictureUrl { get; set; }

            // --- Navigation Properties ---
            // Link to the appointments where this user is the Patient
            [InverseProperty("Patient")]
            public virtual ICollection<Appointment> PatientAppointments { get; set; }

            // Link to the appointments where this user is the Doctor
            [InverseProperty("Doctor")]
            public virtual ICollection<Appointment> DoctorAppointments { get; set; }

        // Link to feedbacks given to this user (if they are a doctor)
        [InverseProperty("Doctor")]
       
        public virtual ICollection<Feedback> ReceivedFeedbacks { get; set; } = new List<Feedback>();
    }   
}
