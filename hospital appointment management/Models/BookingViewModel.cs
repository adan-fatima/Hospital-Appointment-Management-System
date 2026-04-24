using System;
using System.Collections.Generic;

namespace hospital_appointment_management.Models
{
    public class BookingViewModel
    {
        public string DoctorId { get; set; }
        // Holds a list of slots for each of the 7 days
        public Dictionary<DateTime, List<TimeSlot>> WeeklySlots { get; set; } = new Dictionary<DateTime, List<TimeSlot>>();
    }

    public class TimeSlot
    {
        public TimeSpan Time { get; set; }
        public string TimeString => Time.ToString(@"hh\:mm");
        public bool IsAvailable { get; set; }
        public string Status { get; set; } // "Green", "Yellow", or "Red"
    }
}