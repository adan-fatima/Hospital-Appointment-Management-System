using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hospital_appointment_management.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        public DateTime AppointmentDate { get; set; }
        public string AppointmentTime { get; set; } // e.g., "09:30"

        // Use the Enum here instead of a string
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public string? Reason { get; set; }

        public string PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual ApplicationUser Patient { get; set; }

        public string DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual ApplicationUser Doctor { get; set; }

        // Navigation property for Feedback (1-to-1 or 1-to-Many)
        public virtual ICollection<Feedback> Feedbacks { get; set; }
    }
}
