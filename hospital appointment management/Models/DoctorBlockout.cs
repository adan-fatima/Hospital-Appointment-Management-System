using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hospital_appointment_management.Models
{
    public class DoctorBlockout
    {
        [Key] // Primary Key
        public int Id { get; set; }

        // Foreign Key: Links this blockout to a specific Doctor
        [Required]
        public string DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public virtual ApplicationUser Doctor { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime BlockoutDate { get; set; }

        public bool IsFullDay { get; set; }

        // Specific time range for the block (e.g., 13:00 to 14:00)
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }
}