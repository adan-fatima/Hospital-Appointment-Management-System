namespace hospital_appointment_management.Models
{
    public class PatientDashboardViewModel
    {
        public List<Appointment> UpcomingAppointments { get; set; } = new();
        
        public List<AppointmentWithFeedbackVM> PastAppointments { get; set; } = new();

        public List<Appointment> TodayAppointments { get; set; } = new();
    }
}