using hospital_appointment_management.Areas.Identity.Data;
using hospital_appointment_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hospital_appointment_management.Controllers
{
    [Authorize(Roles = "Doctor")]
    [Authorize(Roles = "Doctor")]
    public class DoctorPanel : Controller
    {
        private readonly hospital_appointment_managementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DoctorPanel(hospital_appointment_managementContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.DoctorProfiles.FirstOrDefaultAsync(p => p.DoctorId == userId);

            if (profile == null || !profile.IsProfileComplete)
            {
                return RedirectToAction(nameof(EditProfile));
            }

            // 1. Existing Stats logic...
            ViewBag.TotalUpcoming = await _context.Appointments
                .CountAsync(a => a.DoctorId == userId &&
                            a.AppointmentDate >= DateTime.Today &&
                            a.Status != AppointmentStatus.Cancelled);
            var today = DateTime.Today;

            ViewBag.CompletedToday = await _context.Appointments
                .CountAsync(a =>
                    a.DoctorId == userId &&
                    a.Status == AppointmentStatus.Completed &&
                    a.AppointmentDate.Date == today
                );
           

            ViewBag.CancelledToday = await _context.Appointments
                .CountAsync(a =>
                    a.DoctorId == userId &&
                    a.Status == AppointmentStatus.Cancelled &&
                    a.AppointmentDate.Date == today
                );

            // 2. Fetch Feedbacks for this doctor
            // We include the Patient (Identity User) to show who wrote the feedback
            var feedbacks = await _context.Feedbacks
                .Include(f => f.Patient)
                .Where(f => f.DoctorId == userId)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            ViewBag.Feedbacks = feedbacks;
            ViewBag.TotalReviews = await _context.Feedbacks
    .CountAsync(f => f.DoctorId == userId);

            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == userId)
                .ToListAsync();

            return View(appointments);
        }
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Appointments()
        {
            var doctorId = _userManager.GetUserId(User);
            var today = DateTime.Today;

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var viewModel = new PatientDashboardViewModel
            {
                TodayAppointments = appointments
                    .Where(a => a.AppointmentDate.Date == today)
                    .ToList(),

                UpcomingAppointments = appointments
                    .Where(a => a.AppointmentDate.Date > today)
                    .ToList()
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, AppointmentStatus newStatus)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            var userId = _userManager.GetUserId(User);

            if (appointment != null && appointment.DoctorId == userId)
            {
                // Check if the user is trying to CANCEL
                if (newStatus == AppointmentStatus.Cancelled)
                {
                    // Combine Date and Time into a single DateTime object for comparison
                    // Assuming app.AppointmentTime is a string like "10:30"
                    DateTime appointmentFullDateTime = appointment.AppointmentDate.Date + TimeSpan.Parse(appointment.AppointmentTime);

                    // Logic: Is (AppointmentTime - Now) less than 24 hours?
                    if ((appointmentFullDateTime - DateTime.Now).TotalHours < 24)
                    {
                        TempData["Error"] = "Cancellations are only allowed at least 24 hours in advance.";
                        return RedirectToAction(nameof(Appointments));
                    }
                }

                appointment.Status = newStatus;
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Appointment successfully marked as {newStatus}.";
            }

            return RedirectToAction(nameof(Appointments));
        }
        public async Task<IActionResult> profile()
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.DoctorProfiles.Include(p => p.Doctor).FirstOrDefaultAsync(p => p.DoctorId == userId);

            // If they haven't finished, don't show the profile; send them to the form
            if (profile == null || !profile.IsProfileComplete)
            {
                TempData["Info"] = "Please complete your profile details first.";
                return RedirectToAction(nameof(EditProfile));
            }

            return View(profile);
        }
        // GET: DoctorPanel/EditProfile
        // This loads the page for the doctor
        // 1. THE GET METHOD: Displays the Edit Profile page to the doctor
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = _userManager.GetUserId(User);

            // Attempt to find the existing profile
            var profile = await _context.DoctorProfiles.FirstOrDefaultAsync(p => p.DoctorId == userId);

            // If no profile exists, create a temporary one for the view
            if (profile == null)
            {
                profile = new DoctorProfile { DoctorId = userId };
            }

            return View(profile);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(DoctorProfile model, IFormFile? profileImage)
        {
            var userId = _userManager.GetUserId(User);

            // 1. Fetch both the Profile and the Identity User account
            var profile = await _context.DoctorProfiles.FirstOrDefaultAsync(p => p.DoctorId == userId);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound();

            // 2. Initialize profile if it doesn't exist
            if (profile == null)
            {
                profile = new DoctorProfile
                {
                    DoctorId = userId,
                    // Default path to prevent SQL NULL errors on columns that don't allow nulls
                    ProfileImagePath = "/images/default-doctor.jpg"
                };
                _context.DoctorProfiles.Add(profile);
            }

            // 3. Update Sync Logic: ApplicationUser Table
            // This updates the Identity table (useful for quick lookups and layout headers)
            user.Specialization = model.Specialization;
            user.Bio = model.Bio;

            // Split FullName into First and Last for the Identity Table
            var nameParts = (model.FullName ?? "").Split(' ', 2);
            user.FirstName = nameParts[0];
            user.LastName = nameParts.Length > 1 ? nameParts[1] : "";

            // 4. Update Sync Logic: DoctorProfile Table
            // This updates the professional profile used for the Specialist Cards
            profile.FullName = model.FullName;
            profile.Specialization = model.Specialization;
            profile.Bio = model.Bio;
            profile.ExperienceYears = model.ExperienceYears;

            // 5. Image Handling Logic
            if (profileImage != null && profileImage.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/doctors");

                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                // Update BOTH table image paths to stay in sync
                var savedPath = "/uploads/doctors/" + fileName;
                profile.ProfileImagePath = savedPath;
                user.ProfilePictureUrl = savedPath;
            }

            // 6. Gatekeeper Condition Check
            // A profile is complete only if it has a Bio and a non-default image
            if (!string.IsNullOrEmpty(profile.Bio) &&
                !string.IsNullOrEmpty(profile.FullName) &&
                profile.ProfileImagePath != "/images/default-doctor.jpg")
            {
                profile.IsProfileComplete = true;
            }
            else
            {
                // If they deleted their Bio or similar, reset completeness
                profile.IsProfileComplete = false;
            }

            // 7. Save Changes to both tables
            // UpdateAsync handles AspNetUsers, SaveChangesAsync handles DoctorProfiles
            var identityResult = await _userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                ModelState.AddModelError("", "Failed to update Identity User properties.");
                return View(model);
            }

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Profile synchronized and saved successfully!";
            return RedirectToAction("Index");
        }
        // 2. THE POST METHOD: Saves the data and handles the image upload
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> EditProfile(DoctorProfile model, IFormFile? profileImage)
        //{
        //    var userId = _userManager.GetUserId(User);
        //    var profile = await _context.DoctorProfiles.FirstOrDefaultAsync(p => p.DoctorId == userId);

        //    if (profile == null)
        //    {
        //        profile = new DoctorProfile
        //        {
        //            DoctorId = userId,
        //            // Provide a default value for new records to prevent the SQL error
        //            ProfileImagePath = "/images/default-doctor.jpg"
        //        };
        //        _context.DoctorProfiles.Add(profile);
        //    }

        //    // Update text fields
        //    profile.FullName = model.FullName;
        //    profile.Specialization = model.Specialization;
        //    profile.Bio = model.Bio;
        //    profile.ExperienceYears = model.ExperienceYears;

        //    // Only update the path if a new image is provided
        //    if (profileImage != null && profileImage.Length > 0)
        //    {
        //        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);
        //        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/doctors", fileName);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await profileImage.CopyToAsync(stream);
        //        }

        //        profile.ProfileImagePath = "/uploads/doctors/" + fileName;
        //    }

        //    // Check if profile is complete
        //    if (!string.IsNullOrEmpty(profile.Bio) && profile.ProfileImagePath != "/images/default-doctor.jpg")
        //    {
        //        profile.IsProfileComplete = true;
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction("Index");
        //}
        // GET: DoctorPanel/Availability
        public async Task<IActionResult> Availability()
        {
            var userId = _userManager.GetUserId(User);

            // Fetch all future blockouts for this doctor to show in a list
            var blockouts = await _context.DoctorBlockouts
                .Where(b => b.DoctorId == userId && b.BlockoutDate >= DateTime.Today)
                .OrderBy(b => b.BlockoutDate)
                .ToListAsync();

            return View(blockouts);
        }

        // POST: DoctorPanel/BlockTime
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockTime(DateTime date, TimeSpan? start, TimeSpan? end, bool fullDay)
        {
            if (date < DateTime.Today)
            {
                TempData["Error"] = "You cannot block a date in the past.";
                return RedirectToAction(nameof(Availability));
            }

            var block = new DoctorBlockout
            {
                DoctorId = _userManager.GetUserId(User),
                BlockoutDate = date,
                IsFullDay = fullDay,
                // If it's a full day off, we ignore specific times
                StartTime = fullDay ? null : start,
                EndTime = fullDay ? null : end
            };

            _context.DoctorBlockouts.Add(block);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Time blocked successfully.";
            return RedirectToAction(nameof(Availability));
        }

        // Action to remove a blockout if the doctor changes their mind
        [HttpPost]
        public async Task<IActionResult> DeleteBlockout(int id)
        {
            var block = await _context.DoctorBlockouts.FindAsync(id);
            if (block != null && block.DoctorId == _userManager.GetUserId(User))
            {
                _context.DoctorBlockouts.Remove(block);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Availability));
        }
       
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var profile = await _context.DoctorProfiles
                .Include(p => p.Doctor) // Get FirstName, LastName from ApplicationUser
                    .ThenInclude(u => u.ReceivedFeedbacks) // Get Feedbacks linked to that user
                .FirstOrDefaultAsync(p => p.DoctorId == id);

            if (profile == null) return NotFound();

            return View(profile);
        }
    }
}
