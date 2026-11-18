using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelBuddy.Data;
using TravelBuddy.Models;
using ModelRoute = TravelBuddy.Models.Route;

namespace TravelBuddy.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TravelBuddyDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.EnsureCreatedAsync();

            // Seed Roles
            string[] roles = { "Admin", "Operator", "Passenger" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            var adminEmail = "admin@travelbuddy.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    Status = UserStatus.Active
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Operator User
            var operatorEmail = "conductor1@travelbuddy.local";
            var operatorUser = await userManager.FindByEmailAsync(operatorEmail);
            if (operatorUser == null)
            {
                operatorUser = new ApplicationUser
                {
                    UserName = operatorEmail,
                    Email = operatorEmail,
                    FullName = "Ramesh Kumar (Conductor)",
                    EmailConfirmed = true,
                    Status = UserStatus.Active
                };
                var result = await userManager.CreateAsync(operatorUser, "Operator@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(operatorUser, "Operator");
                    context.Operators.Add(new Operator
                    {
                        UserId = operatorUser.Id,
                        OperatorCode = "OP-101",
                        Status = "Active"
                    });
                    await context.SaveChangesAsync();
                }
            }

            // Seed Demo Passenger User
            var passengerEmail = "passenger@travelbuddy.local";
            var passengerUser = await userManager.FindByEmailAsync(passengerEmail);
            if (passengerUser == null)
            {
                passengerUser = new ApplicationUser
                {
                    UserName = passengerEmail,
                    Email = passengerEmail,
                    FullName = "Priya Sharma",
                    EmailConfirmed = true,
                    PhoneNumber = "+91 9876543210",
                    Status = UserStatus.Active
                };
                var result = await userManager.CreateAsync(passengerUser, "Passenger@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(passengerUser, "Passenger");
                }
            }

            // Seed Routes and Stops
            if (!context.Routes.Any())
            {
                var r101 = new ModelRoute
                {
                    RouteCode = "R-101",
                    Name = "Route 101 - City Center to University",
                    Description = "Major central corridor connecting railway station, tech park and university.",
                    Status = "Published"
                };

                var r202 = new ModelRoute
                {
                    RouteCode = "R-202",
                    Name = "Route 202 - Metro Express Outer Ring",
                    Description = "High frequency express ring road connector.",
                    Status = "Published"
                };

                context.Routes.AddRange(r101, r202);
                await context.SaveChangesAsync();

                var stop1 = new Stop { StopCode = "ST-01", Name = "Central Railway Station", Latitude = 12.9716, Longitude = 77.5946 };
                var stop2 = new Stop { StopCode = "ST-02", Name = "City Plaza & Market", Latitude = 12.9750, Longitude = 77.5990 };
                var stop3 = new Stop { StopCode = "ST-03", Name = "Infotech Cyber Park", Latitude = 12.9810, Longitude = 77.6050 };
                var stop4 = new Stop { StopCode = "ST-04", Name = "State University Campus", Latitude = 12.9900, Longitude = 77.6150 };

                context.Stops.AddRange(stop1, stop2, stop3, stop4);
                await context.SaveChangesAsync();

                context.RouteStops.AddRange(
                    new RouteStop { RouteId = r101.RouteId, StopId = stop1.StopId, StopSequence = 1 },
                    new RouteStop { RouteId = r101.RouteId, StopId = stop2.StopId, StopSequence = 2 },
                    new RouteStop { RouteId = r101.RouteId, StopId = stop3.StopId, StopSequence = 3 },
                    new RouteStop { RouteId = r101.RouteId, StopId = stop4.StopId, StopSequence = 4 }
                );
                await context.SaveChangesAsync();
            }

            // Seed Fare Products per specification
            if (!context.FareProducts.Any())
            {
                var fpSingle = new FareProduct
                {
                    Name = "Single Journey Pass",
                    Description = "One boarding for one journey across any eligible route.",
                    PassType = PassType.SingleJourney,
                    Price = 30.00m,
                    ValidityValue = 1,
                    ValidityUnit = "Journeys",
                    CoverageType = "AllRoutes",
                    Status = FareProductStatus.Published
                };

                var fpDaily = new FareProduct
                {
                    Name = "Daily Unlimited Pass",
                    Description = "Unlimited boardings on all eligible city routes for one calendar day.",
                    PassType = PassType.Daily,
                    Price = 70.00m,
                    ValidityValue = 1,
                    ValidityUnit = "Days",
                    CoverageType = "AllRoutes",
                    Status = FareProductStatus.Published
                };

                var fpWeekly = new FareProduct
                {
                    Name = "Weekly Commuter Pass",
                    Description = "Unlimited boardings on all eligible routes for 7 consecutive days.",
                    PassType = PassType.Weekly,
                    Price = 350.00m,
                    ValidityValue = 7,
                    ValidityUnit = "Days",
                    CoverageType = "AllRoutes",
                    Status = FareProductStatus.Published
                };

                var fpMonthly = new FareProduct
                {
                    Name = "Monthly Freedom Pass",
                    Description = "Unlimited boardings on all city routes for 30 consecutive days.",
                    PassType = PassType.Monthly,
                    Price = 999.00m,
                    ValidityValue = 30,
                    ValidityUnit = "Days",
                    CoverageType = "AllRoutes",
                    Status = FareProductStatus.Published
                };

                context.FareProducts.AddRange(fpSingle, fpDaily, fpWeekly, fpMonthly);
                await context.SaveChangesAsync();
            }
        }
    }
}
