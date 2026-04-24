using hospital_appointment_management.Areas.Identity.Data;
using hospital_appointment_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hospital_appointment_management.Controllers
{
    [Authorize(Roles = "Patient")]
    public class FeedbacksController : Controller
    {
        private readonly hospital_appointment_managementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FeedbacksController(hospital_appointment_managementContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Create Feedback
        public async Task<IActionResult> Create(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            // Safety: Only allow if appointment is Completed and belongs to current user
            if (appointment == null || appointment.Status != AppointmentStatus.Completed ||
                appointment.PatientId != _userManager.GetUserId(User))
            {
                return BadRequest("You can only review completed appointments.");
            }

            ViewBag.DoctorName = $"Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}";
            return View(new Feedback { AppointmentId = appointmentId, DoctorId = appointment.DoctorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Feedback feedback)
        {
            feedback.PatientId = _userManager.GetUserId(User);
            feedback.CreatedDate = DateTime.Now;

            var exists = await _context.Feedbacks
                .AnyAsync(f => f.AppointmentId == feedback.AppointmentId);

            if (exists)
                return BadRequest("Feedback already submitted.");

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard", "Appointments");
        }

    }
}
