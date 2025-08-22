using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EquityPerformanceTracker.Data.Context;
using EquityPerformanceTracker.Core.Interfaces;
using EquityPerformanceTracker.Core.Models;
using EquityPerformanceTracker.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddViewOptions(options =>
    {
        options.HtmlHelperOptions.ClientValidationEnabled = true;
    });
builder.Services.AddRazorPages(); // Needed for Identity UI

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity Services - FIXED: Use ApplicationUser instead of IdentityUser
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings for public use
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false; // Changed to false for easier development
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6; // Reduced from 8 for easier development
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Email confirmation (set to false for development)
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI() // This provides the default Identity UI pages
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Remember me for 30 days
    options.SlidingExpiration = true;
});

// Add custom services
builder.Services.AddScoped<IAlpacaService, AlpacaService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSubscription", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
              {
                  // This will be implemented in a custom requirement
                  return true; // Placeholder
              }));
});

// Add background services for price updates (commented out for now)
// builder.Services.AddHostedService<PriceUpdateBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map Identity pages
app.MapRazorPages();

// API Controllers (move this here)
app.MapControllers();

// Add Serilog request logging
app.UseSerilogRequestLogging();

// Ensure database is created and seeded (wrapped in try-catch for safety)
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await DatabaseSeeder.SeedAsync(context, userManager, roleManager);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database");
}

app.Run();