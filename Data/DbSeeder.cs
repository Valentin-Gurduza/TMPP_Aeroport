using Microsoft.AspNetCore.Identity;
using TMPP_Aeroport.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
            
            // Clear existing for a fresh simulation start (Optional, but good for demo)
            if (context.Flights.Any()) 
            { 
                context.Flights.RemoveRange(context.Flights); 
                await context.SaveChangesAsync(); 
            }
            if (context.Aircrafts.Any()) 
            { 
                context.Aircrafts.RemoveRange(context.Aircrafts); 
                await context.SaveChangesAsync(); 
            }
            
            if (!context.Aircrafts.Any())
            {
                var aircrafts = new List<Aircraft>
                {
                    new Aircraft { RegistrationCode = "YR-BGG", Model = "Boeing 737-800", Capacity = 189, Airline = "TAROM" },
                    new Aircraft { RegistrationCode = "YR-BGH", Model = "Boeing 737-800", Capacity = 189, Airline = "TAROM" },
                    new Aircraft { RegistrationCode = "YR-BGI", Model = "Boeing 737-700", Capacity = 148, Airline = "TAROM" },
                    new Aircraft { RegistrationCode = "F-GKXN", Model = "Airbus A320", Capacity = 174, Airline = "Air France" },
                    new Aircraft { RegistrationCode = "F-GKXO", Model = "Airbus A321", Capacity = 212, Airline = "Air France" },
                    new Aircraft { RegistrationCode = "PH-BXO", Model = "Boeing 737-800", Capacity = 186, Airline = "KLM" },
                    new Aircraft { RegistrationCode = "PH-BXP", Model = "Boeing 737-900", Capacity = 204, Airline = "KLM" },
                    new Aircraft { RegistrationCode = "G-EUUI", Model = "Airbus A320", Capacity = 180, Airline = "British Airways" },
                    new Aircraft { RegistrationCode = "D-AIZE", Model = "Airbus A320", Capacity = 168, Airline = "Lufthansa" },
                    new Aircraft { RegistrationCode = "EC-MKL", Model = "Airbus A320", Capacity = 180, Airline = "Iberia" },
                    new Aircraft { RegistrationCode = "EI-DCL", Model = "Boeing 737-800", Capacity = 189, Airline = "Ryanair" },
                    new Aircraft { RegistrationCode = "HA-LPM", Model = "Airbus A320", Capacity = 180, Airline = "Wizz Air" }
                };
                context.Aircrafts.AddRange(aircrafts);
                await context.SaveChangesAsync();
            }

            if (!context.Flights.Any())
            {
                var aircraftList = context.Aircrafts.ToList();
                if (aircraftList.Count > 0)
                {
                    var now = DateTime.Now;
                    var flights = new List<Flight>
                    {
                        // Airborne right now (Departed 1 hour ago)
                        new Flight { FlightNumber = "RO301", Destination = "Frankfurt FRA", DepartureTime = now.AddHours(-1), Status = "Airborne", AircraftId = aircraftList[0].Id },
                        new Flight { FlightNumber = "AF108", Destination = "Bucharest OTP", DepartureTime = now.AddHours(-1.5), Status = "Airborne", AircraftId = aircraftList[3].Id },
                        new Flight { FlightNumber = "KLM99", Destination = "Amsterdam AMS", DepartureTime = now.AddMinutes(-45), Status = "Airborne", AircraftId = aircraftList[5].Id },
                        new Flight { FlightNumber = "BA402", Destination = "Rome FCO", DepartureTime = now.AddMinutes(-120), Status = "Airborne", AircraftId = aircraftList[7].Id },
                        
                        // Boarding right now (Departing in 15 mins)
                        new Flight { FlightNumber = "RO381", Destination = "Paris CDG", DepartureTime = now.AddMinutes(15), Status = "Boarding", AircraftId = aircraftList[1].Id },
                        new Flight { FlightNumber = "LH202", Destination = "Berlin BER", DepartureTime = now.AddMinutes(5), Status = "Boarding", AircraftId = aircraftList[8].Id },
                        new Flight { FlightNumber = "W6314", Destination = "London LHR", DepartureTime = now.AddMinutes(25), Status = "Boarding", AircraftId = aircraftList[11].Id },

                        // Scheduled soon
                        new Flight { FlightNumber = "RO361", Destination = "Amsterdam AMS", DepartureTime = now.AddMinutes(45), Status = "Scheduled", AircraftId = aircraftList[2].Id },
                        new Flight { FlightNumber = "IB311", Destination = "Madrid MAD", DepartureTime = now.AddMinutes(90), Status = "Scheduled", AircraftId = aircraftList[9].Id },
                        new Flight { FlightNumber = "FR102", Destination = "Rome FCO", DepartureTime = now.AddHours(2), Status = "Scheduled", AircraftId = aircraftList[10].Id },
                        
                        // Scheduled future (Tomorrow)
                        new Flight { FlightNumber = "RO315", Destination = "Munich MUC", DepartureTime = now.AddDays(1).AddHours(2), Status = "Scheduled", AircraftId = aircraftList[0].Id },
                        new Flight { FlightNumber = "RO391", Destination = "London LHR", DepartureTime = now.AddDays(1).AddHours(6), Status = "Scheduled", AircraftId = aircraftList[1].Id },
                        new Flight { FlightNumber = "AF202", Destination = "Paris CDG", DepartureTime = now.AddDays(2).AddHours(1), Status = "Scheduled", AircraftId = aircraftList[4].Id },
                        new Flight { FlightNumber = "KLM101", Destination = "Amsterdam AMS", DepartureTime = now.AddDays(3), Status = "Scheduled", AircraftId = aircraftList[6].Id }
                    };
                    context.Flights.AddRange(flights);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
