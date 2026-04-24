using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
//namespace hospital_appointment_management.Models;
using Microsoft.EntityFrameworkCore;
using hospital_appointment_management.Areas.Identity.Data;
namespace hospital_appointment_management.Models
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("hospital_appointment_managementContextConnection") ?? throw new InvalidOperationException("Connection string 'hospital_appointment_managementContextConnection' not found.");

            builder.Services.AddDbContext<hospital_appointment_managementContext>(options => options.UseSqlServer(connectionString));

            //  builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddRoles<IdentityRole>().AddEntityFrameworkStores<hospital_appointment_managementContext>();
            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;

                // Password rules
                options.Password.RequireDigit = false;           // number not required
                options.Password.RequireLowercase = false;       // lowercase not required
                options.Password.RequireUppercase = false;       // uppercase not required
                options.Password.RequireNonAlphanumeric = false; // special character not required
                options.Password.RequiredLength = 6;             // minimum length
                options.Password.RequiredUniqueChars = 1;        // how many unique characters
            })
  .AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<hospital_appointment_managementContext>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    // Call the seeder method
                    await DbSeeder.SeedRolesAndAdminAsync(services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();
            await app.RunAsync();
        }
    }
}
