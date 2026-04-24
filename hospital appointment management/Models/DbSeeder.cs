using hospital_appointment_management.Models;
using Microsoft.AspNetCore.Identity;

namespace hospital_appointment_management.Models
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            // Get the Managers from the Service Provider
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Define the Roles your system needs
            string[] roleNames = { "Admin", "Doctor", "Patient" };

            foreach (var roleName in roleNames)
            {
                // Check if the role already exists in dbo.AspNetRoles
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Create the role
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Create a default Admin User
            var adminEmail = "admin@hospital.com";
            var user = await userManager.FindByEmailAsync(adminEmail);

            if (user == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true // Crucial if you have RequireConfirmedAccount = true
                };

                // Create the user with a strong password
                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin@123");
                if (createPowerUser.Succeeded)
                {
                    // Assign the "Admin" role to this user in dbo.AspNetUserRoles
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}