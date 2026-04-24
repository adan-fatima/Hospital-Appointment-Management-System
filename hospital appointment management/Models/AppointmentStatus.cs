namespace hospital_appointment_management.Models
{
    public enum AppointmentStatus
    {
        Pending,   // 🟡 Initial state
        Approved,  // 🔵 Confirmed by Doctor
        Completed, // 🟢 Finished/Auto-completed
        Cancelled,  // 🔴 Rejected or Cancelled
        Confirmed
    }
}