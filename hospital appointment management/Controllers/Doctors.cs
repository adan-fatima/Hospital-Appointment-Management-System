using hospital_appointment_management.Areas.Identity.Data;
using hospital_appointment_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hospital_appointment_management.Controllers
{
    public class Doctors : Controller
    {        private readonly hospital_appointment_managementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public Doctors(hospital_appointment_managementContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
       
        public async Task<IActionResult> Index()
        {
            // Fetch only profiles where the doctor is approved AND has completed their profile
            var profiles = await _context.DoctorProfiles
                .Include(p => p.Doctor)
                .Where(p => p.Doctor.IsApproved && p.IsProfileComplete)
                .ToListAsync();

            return View(profiles);
        }

        public async Task<IActionResult> Details(string id)
        {
            var profile = await _context.DoctorProfiles
                .Include(p => p.Doctor)
                .ThenInclude(u => u.ReceivedFeedbacks)
                .FirstOrDefaultAsync(p => p.DoctorId == id);

            if (profile == null) return NotFound();

            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> SearchDoctors(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            // ✅ Get all users in Doctor role
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");

            var filtered = doctors
                .Where(d =>
                    d.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.LastName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.Specialization.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(6)
                .Select(d => new
                {
                    id = d.Id,
                    name = d.FirstName + " " + d.LastName,
                    specialization = d.Specialization
                });

            return Json(filtered);
        }


    }
}

