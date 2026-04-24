using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hospital_appointment_management.Models
{
    public class DoctorProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string DoctorId { get; set; } // FK to ApplicationUser

        [ForeignKey("DoctorId")]
        public virtual ApplicationUser Doctor { get; set; }

        public string FullName { get; set; }
        public string? Specialization { get; set; }
        public string? Bio { get; set; }
        public int ExperienceYears { get; set; }
        public bool IsProfileComplete { get; set; } = false;
        public string? ProfileImagePath { get; set; }  // The gatekeeper
    }
}
