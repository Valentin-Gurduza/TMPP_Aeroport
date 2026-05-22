using Microsoft.AspNetCore.Identity;
using TMPP_Aeroport.Models;

namespace TMPP_Aeroport.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Admin", "ATC_Manager", "Ground_Staff", "Passenger" };

            // Seed Roles
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            var adminUser = await userManager.FindByEmailAsync("admin@sams.local");
            if (adminUser == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = "admin@sams.local",
                    Email = "admin@sams.local",
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true
                };

                var createPowerUser = await userManager.CreateAsync(newAdmin, "Admin123!");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }

            // Seed Aircrafts and Flights
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            if (!context.Aircrafts.Any())
            {
                var aircrafts = new List<Aircraft>
                {
                    new Aircraft { RegistrationCode = "YR-BGG", Model = "Boeing 737-800", Capacity = 189, Airline = "TAROM" },
                    new Aircraft { RegistrationCode = "YR-BGH", Model = "Boeing 737-800", Capacity = 189, Airline = "TAROM" },
                    new Aircraft { RegistrationCode = "YR-BGI", Model = "Boeing 737-800", Capacity = 189, Airline = "TAROM" }
                };
                context.Aircrafts.AddRange(aircrafts);
                await context.SaveChangesAsync();
            }

            if (!context.Flights.Any())
            {
                var firstAircraft = context.Aircrafts.FirstOrDefault();
                if (firstAircraft != null)
                {
                    var flights = new List<Flight>
                    {
                        new Flight { FlightNumber = "RO301", Destination = "Frankfurt FRA", DepartureTime = DateTime.Now.AddHours(2), Status = "Scheduled", AircraftId = firstAircraft.Id },
                        new Flight { FlightNumber = "RO381", Destination = "Paris CDG", DepartureTime = DateTime.Now.AddHours(3), Status = "Scheduled", AircraftId = firstAircraft.Id },
                        new Flight { FlightNumber = "RO361", Destination = "Amsterdam AMS", DepartureTime = DateTime.Now.AddHours(4), Status = "Boarding", AircraftId = firstAircraft.Id },
                        new Flight { FlightNumber = "RO315", Destination = "Munich MUC", DepartureTime = DateTime.Now.AddHours(5), Status = "Scheduled", AircraftId = firstAircraft.Id },
                        new Flight { FlightNumber = "RO391", Destination = "London LHR", DepartureTime = DateTime.Now.AddHours(6), Status = "Scheduled", AircraftId = firstAircraft.Id }
                    };
                    context.Flights.AddRange(flights);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
