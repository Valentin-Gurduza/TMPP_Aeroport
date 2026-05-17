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

// DIP (Dependency Inversion Principle): Modulele de nivel înalt (Controllers) nu trebuie să depindă de cele de nivel jos (Services), ci de abstracții.
builder.Services.AddSingleton<TMPP_Aeroport.Domain.Interfaces.IFlightService, TMPP_Aeroport.Services.FlightService>();
builder.Services.AddSingleton<TMPP_Aeroport.Domain.Interfaces.IAircraftService, TMPP_Aeroport.Services.AircraftService>();

// Register Adapter Pattern Services
builder.Services.AddSingleton<TMPP_Aeroport.Domain.Adapter.LegacyWeatherSystem>();
builder.Services.AddSingleton<TMPP_Aeroport.Domain.Adapter.IAirportWeatherService, TMPP_Aeroport.Domain.Adapter.WeatherAdapter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await TMPP_Aeroport.Data.DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
