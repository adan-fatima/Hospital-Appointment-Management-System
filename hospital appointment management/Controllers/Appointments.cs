using hospital_appointment_management.Areas.Identity.Data;
using hospital_appointment_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hospital_appointment_management.Models
{
    [Authorize]
    public class Appointments : Controller
    {
        private readonly hospital_appointment_managementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public Appointments(hospital_appointment_managementContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

       

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Index(string doctorId)
        {
            if (string.IsNullOrEmpty(doctorId)) return RedirectToAction("Index", "Doctors");

            var model = new BookingViewModel { DoctorId = doctorId };
            var today = DateTime.Today;

            // 1. Fetch data from DB
            var existingBookings = await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();

            var doctorBlockouts = await _context.DoctorBlockouts
                .Where(b => b.DoctorId == doctorId)
                .ToListAsync();

            // 2. Generate 7-Day Logic
            for (int i = 0; i < 7; i++)
            {
                var date = today.AddDays(i);
                var slots = GenerateDefaultSlots(date);

                foreach (var slot in slots)
                {
                    // Logic for RED: Doctor manually blocked this time
                    bool isBlocked = doctorBlockouts.Any(b => b.BlockoutDate.Date == date.Date &&
                                     (b.IsFullDay || (slot.Time >= b.StartTime && slot.Time < b.EndTime)));

                    // Logic for YELLOW: Another patient already booked this
                    bool isBooked = existingBookings.Any(a => a.AppointmentDate.Date == date.Date &&
                                    a.AppointmentTime == slot.TimeString);

                    if (isBlocked)
                    {
                        slot.Status = "Red"; // Unavailable
                        slot.IsAvailable = false;
                    }
                    else if (isBooked)
                    {
                        slot.Status = "Yellow"; // Booked
                        slot.IsAvailable = false;
                    }
                    else
                    {
                        slot.Status = "Green"; // Available!
                        slot.IsAvailable = true;
                    }
                }
                model.WeeklySlots.Add(date, slots);
            }

            return View(model);
        }

        // 1. Show the Summary Page
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Book(string doctorId, DateTime date, string time)
        {
            var doctor = await _context.Users.FindAsync(doctorId);
            if (doctor == null) return NotFound();

            // We pass these details to the view so the user can see them
            ViewBag.DoctorName = $"Dr. {doctor.FirstName} {doctor.LastName}";
            ViewBag.Specialization = doctor.Specialization;
            ViewBag.Date = date;
            ViewBag.Time = time;
            ViewBag.DoctorId = doctorId;

            return View();
        }

        // 2. The Final Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> ConfirmBooking(string doctorId, DateTime date, string time)
        {
            // Safety Net: Double-check if the slot was taken while the user was on the confirm page
            bool alreadyTaken = await _context.Appointments.AnyAsync(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate.Date == date.Date &&
                a.AppointmentTime == time &&
                a.Status != AppointmentStatus.Cancelled);

            if (alreadyTaken)
            {
                TempData["Error"] = "Sorry, this slot was just booked by someone else. Please choose another.";
                return RedirectToAction("Index", new { doctorId = doctorId });
            }

            var appointment = new Appointment
            {
                DoctorId = doctorId,
                PatientId = _userManager.GetUserId(User),
                AppointmentDate = date,
                AppointmentTime = time,
                Status = AppointmentStatus.Confirmed // Auto-confirmed as per your plan!
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Appointment confirmed successfully!";
            return RedirectToAction("Dashboard"); // Move to Phase 5 next
        }
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Dashboard()
        {
            var patientId = _userManager.GetUserId(User);
            var now = DateTime.Now;

            var candidateAppointments = await _context.Appointments
     .Where(a =>
         a.PatientId == patientId &&
         a.Status == AppointmentStatus.Confirmed &&
         a.AppointmentDate <= now.Date
     )
     .ToListAsync(); // ← move to memory here
            var expiredAppointments = candidateAppointments
                .Where(a =>
                    a.AppointmentDate < now.Date ||
                    (
                        a.AppointmentDate == now.Date &&
                        TimeSpan.Parse(a.AppointmentTime) <= now.TimeOfDay
                    )
                )
                .ToList();

            foreach (var appt in expiredAppointments)
            {
                appt.Status = AppointmentStatus.Completed;
            }

            if (expiredAppointments.Any())
            {
                await _context.SaveChangesAsync();
            }

            var today = DateTime.Today;

            var allAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var feedbackAppointmentIds = await _context.Feedbacks
            .Where(f => f.PatientId == patientId)
              .Select(f => f.AppointmentId)
                 .ToListAsync();

            var viewModel = new PatientDashboardViewModel
            {
                // Upcoming: Today onwards AND not Cancelled
                UpcomingAppointments = allAppointments
                    .Where(a => a.AppointmentDate >= today && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Completed)
                    .ToList(),

                // Past: Date has passed OR it was Cancelled
                PastAppointments = allAppointments
    .Where(a => a.AppointmentDate <= today &&( a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.Completed))
    .Select(a => new AppointmentWithFeedbackVM
    {
        Appointment = a,
        HasFeedback = feedbackAppointmentIds.Contains(a.Id)
    })
    .ToList()
            };
            
    return View(viewModel);
        }


      

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            var patientId = _userManager.GetUserId(User);

            // Safety: Ensure only the owner can cancel their own appointment
            if (appointment != null && appointment.PatientId == patientId)
            {
                appointment.Status = AppointmentStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Appointment cancelled. The slot is now available for others.";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        private List<TimeSlot> GenerateDefaultSlots(DateTime date)
        {
            var slots = new List<TimeSlot>();
            var startTime = new TimeSpan(9, 0, 0);
            var endTime = new TimeSpan(17, 0, 0);

            while (startTime < endTime)
            {
                slots.Add(new TimeSlot { Time = startTime, IsAvailable = true });
                startTime = startTime.Add(TimeSpan.FromMinutes(30));
            }
            return slots;
        }
    }
} 

// Only one bracket here to close the namespace//using hospital_appointment_management.Areas.Identity.Data;
//using hospital_appointment_management.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace hospital_appointment_management.Models
//{
//    [Authorize]
//    public class Appointments : Controller
//    {
//        private readonly hospital_appointment_managementContext _context;
//        private readonly UserManager<ApplicationUser> _userManager;
//        // 2. Inject it through the constructor
//        public Appointments(hospital_appointment_managementContext context, UserManager<ApplicationUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }
//        [HttpPost]
//        [Authorize(Roles = "Patient")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> ConfirmBooking(string doctorId, DateTime date, string time)
//        {
//            var appointment = new Appointment
//            {
//                DoctorId = doctorId,
//                PatientId = _userManager.GetUserId(User),
//                AppointmentDate = date,
//                AppointmentTime = time,
//                Status = AppointmentStatus.Pending
//            };

//            _context.Appointments.Add(appointment);
//            await _context.SaveChangesAsync();

//            return RedirectToAction(nameof(Index));
//        }



//        [Authorize(Roles = "Patient")]
//        public async Task<IActionResult> Index()
//        {
//            // 1. Get the ID of the logged-in Patient
//            var patientId = _userManager.GetUserId(User);

//            // 2. Fetch all appointments for this patient
//            var myAppointments = await _context.Appointments
//                .Include(a => a.Doctor) // Include Doctor details to show the name
//                .Where(a => a.PatientId == patientId)
//                .OrderByDescending(a => a.AppointmentDate) // Show newest first
//                .ToListAsync();

//            // 3. Return the list to the view
//            return View(myAppointments);
//        }


//        public async Task<IActionResult> Book(string doctorId)
//        {
//            var model = new BookingViewModel { DoctorId = doctorId };
//            var today = DateTime.Today;

//            // 1. Get existing appointments and blockouts for this doctor
//            var existingAppointments = await _context.Appointments
//                .Where(a => a.DoctorId == doctorId && a.Status != AppointmentStatus.Cancelled)
//                .ToListAsync();

//            var blockouts = await _context.DoctorBlockouts
//                .Where(b => b.DoctorId == doctorId)
//                .ToListAsync();

//            // 2. Generate 7 Days, each with 30-minute slots
//            for (int i = 0; i < 7; i++)
//            {
//                var date = today.AddDays(i);
//                var daySlots = GenerateDefaultSlots(date); // 09:00, 09:30, etc.

//                foreach (var slot in daySlots)
//                {
//                    // Check if blocked by Doctor
//                    bool isBlocked = blockouts.Any(b => b.BlockoutDate.Date == date.Date &&
//                                     (b.IsFullDay || (slot.Time >= b.StartTime && slot.Time < b.EndTime)));

//                    // Check if already booked
//                    bool isBooked = existingAppointments.Any(a => a.AppointmentDate.Date == date.Date &&
//                                    a.AppointmentTime == slot.TimeString);

//                    slot.IsAvailable = !isBlocked && !isBooked;
//                }
//                model.WeeklySlots.Add(date, daySlots);
//            }

//            return View(model);
//        }
//        private List<TimeSlot> GenerateDefaultSlots(DateTime date)
//        {
//            var slots = new List<TimeSlot>();
//            var startTime = new TimeSpan(9, 0, 0); // 9:00 AM
//            var endTime = new TimeSpan(17, 0, 0);  // 5:00 PM

//            while (startTime < endTime)
//            {
//                slots.Add(new TimeSlot
//                {
//                    Time = startTime,
//                    IsAvailable = true
//                });
//                startTime = startTime.Add(TimeSpan.FromMinutes(30)); // 30-min intervals
//            }
//            return slots;
//        }
//    }

//}
