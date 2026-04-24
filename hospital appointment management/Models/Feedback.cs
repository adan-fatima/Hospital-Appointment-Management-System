using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hospital_appointment_management.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        // NEW: Link directly to the specific appointment
        [Required]
        public int AppointmentId { get; set; }
        [ForeignKey("AppointmentId")]
        public virtual Appointment Appointment { get; set; }

        [Required]
        public string Comments { get; set; }

        [Range(1, 5)]
        public int StarRating { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // --- Foreign Key: The Doctor receiving the feedback ---
        public string DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public virtual ApplicationUser Doctor { get; set; }

        // --- Foreign Key: The Patient writing the feedback ---
        public string PatientId { get; set; }

        [ForeignKey("PatientId")]
        public virtual ApplicationUser Patient { get; set; }
    }
}
