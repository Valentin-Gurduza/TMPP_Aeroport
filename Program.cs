using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TMPP_Aeroport.Data;
using TMPP_Aeroport.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Airport/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(); // Adăugat pentru Faza 4 (Real-time WebSockets)

// Removed old in-memory services

// Register Object Pool - Ground Support Equipment Fleet (must be before FlightSimulationService)
builder.Services.AddSingleton<TMPP_Aeroport.Domain.ObjectPool.GroundVehiclePool>();

// Global Background Simulation (Registered as Singleton so it can be injected in Controllers, and HostedService to run in background)
builder.Services.AddSingleton<TMPP_Aeroport.Services.FlightSimulationService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<TMPP_Aeroport.Services.FlightSimulationService>());

// Register Adapter Pattern Services
builder.Services.AddSingleton<TMPP_Aeroport.Domain.Adapter.LegacyWeatherSystem>();
builder.Services.AddSingleton<TMPP_Aeroport.Domain.Adapter.IAirportWeatherService, TMPP_Aeroport.Domain.Adapter.WeatherAdapter>();

// Register Mediator Pattern ATCTower as Singleton so state persists across requests
builder.Services.AddSingleton<TMPP_Aeroport.Domain.Mediator.IATCMediator, TMPP_Aeroport.Domain.Mediator.ATCTower>();

// Register Flyweight Factory as Singleton to share aircraft models globally
builder.Services.AddSingleton<TMPP_Aeroport.Domain.Flyweight.AircraftModelFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Airport}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();
app.MapHub<TMPP_Aeroport.Hubs.FlightHub>("/flightHub"); // Endpoint-ul pentru WebSockets

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await TMPP_Aeroport.Data.DbSeeder.SeedRolesAndAdminAsync(services);
        
        // Unified logging: wire Singleton AirportLogger to DB persistence
        TMPP_Aeroport.Domain.Singleton.AirportLogger.Instance.Configure(
            app.Services.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>()
        );
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
