using hospital_appointment_management.Areas.Identity.Data;
using hospital_appointment_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hospital_appointment_management.Controllers
{
    [Authorize(Roles = "Admin")] // Restrict access to Admins only
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly hospital_appointment_managementContext _context;

        public AdminController(UserManager<ApplicationUser> userManager,hospital_appointment_managementContext context)
        {
            _context = context;
            _userManager = userManager;
        }
      

        // Displays all Doctors waiting for approval
        public async Task<IActionResult> Index()
        {
            // Get all users in the Doctor role
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");

            // Filter for those where IsApproved is false
            var pendingDoctors = doctors.Where(d => !d.IsApproved).ToList();

            return View(pendingDoctors);
        }
        public async Task<IActionResult> ManageDoctors()
        {
            var allDoctors = await _userManager.GetUsersInRoleAsync("Doctor");
            var activeDoctors = allDoctors.Where(d => d.IsApproved).ToList();
            return View(activeDoctors);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDoctor(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // 1. Update Approval Status
            user.IsApproved = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // 2. Assign the "Doctor" Role
                // This links the Identity Role to the ApplicationUser
                if (!await _userManager.IsInRoleAsync(user, "Doctor"))
                {
                    await _userManager.AddToRoleAsync(user, "Doctor");
                }

                // 3. Initialize the DoctorProfile
                var profileExists = await _context.DoctorProfiles.AnyAsync(p => p.DoctorId == id);
                if (!profileExists)
                {
                    var newProfile = new DoctorProfile
                    {
                        DoctorId = user.Id, // Link to ApplicationUser
                        FullName = $"{user.FirstName} {user.LastName}",
                        Specialization = user.Specialization,
                        IsProfileComplete = false // Default as per your model
                    };

                    _context.DoctorProfiles.Add(newProfile);
                    await _context.SaveChangesAsync();
                }

                TempData["StatusMessage"] = $"Doctor {user.FirstName} approved and role assigned.";
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisapproveDoctor(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Option A: Delete the user entirely
                var result = await _userManager.DeleteAsync(user);

                // Option B: If you have a property like user.IsRejected, use:
                // user.IsApproved = false;
                // var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["StatusMessage"] = $"Doctor {user.FirstName} {user.LastName} has been removed/disapproved.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error occurred while disapproving the doctor.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // New: Remove a doctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDoctor(string id)
        {
            var doctor = await _userManager.FindByIdAsync(id);
            if (doctor != null)
            {
                var result = await _userManager.DeleteAsync(doctor);
                if (result.Succeeded)
                {
                    TempData["StatusMessage"] = $"Doctor {doctor.FirstName} {doctor.LastName} has been removed from the system.";
                }
            }
            return RedirectToAction(nameof(ManageDoctors));
        }
    }
}