// Program.cs
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Services;
using HamroGrocery.Services.Interfaces;
using HamroGrocery.Services.Providers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// LOGGING CONFIGURATION
// ============================================================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

// ============================================================================
// DATABASE CONFIGURATION
// ============================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 21)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });

    // Enable sensitive data logging in development only
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ============================================================================
// SESSION CONFIGURATION (MUST BE BEFORE IDENTITY)
// ============================================================================
builder.Services.AddDistributedMemoryCache(); // Required for session storage

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24); // Cart persists for 24 hours
    options.Cookie.HttpOnly = true; // Security: prevent JavaScript access
    options.Cookie.IsEssential = true; // GDPR: Essential for cart functionality
    options.Cookie.Name = ".HamroGrocery.Session";
    options.Cookie.SameSite = SameSiteMode.Lax; // Security: CSRF protection

    // Environment-specific cookie security
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP in dev
    }
    else
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Require HTTPS in production
    }
});

// HTTP Context Accessor (Required for accessing HttpContext in services)
builder.Services.AddHttpContextAccessor();

// ============================================================================
// IDENTITY CONFIGURATION
// ============================================================================
builder.Services.AddDefaultIdentity<User>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // Token settings
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
})
.AddRoles<IdentityRole<int>>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// ============================================================================
// COOKIE CONFIGURATION
// ============================================================================
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";

    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".HamroGrocery.Auth";

    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    }
    else
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }

    // Custom redirect after login based on user role
    options.Events.OnSignedIn = async context =>
    {
        var user = context.Principal;
        var logService = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<Program>>();

        if (user?.Identity?.IsAuthenticated == true)
        {
            var userName = user.Identity.Name;

            if (user.IsInRole("Admin") || user.IsInRole("SuperAdmin"))
            {
                logService.LogInformation("Admin user {UserName} signed in, redirecting to admin panel", userName);
                context.Response.Redirect("/Admin");
            }
            else
            {
                logService.LogInformation("Customer {UserName} signed in, redirecting to home", userName);
                context.Response.Redirect("/");
            }
        }
    };
});

// ============================================================================
// AUTHORIZATION POLICIES
// ============================================================================
builder.Services.AddAuthorization(options =>
{
    // Admin policies
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));

    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("SuperAdmin"));

    // Management policies
    options.AddPolicy("ManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin", "SuperAdmin"));

    options.AddPolicy("StaffOrAbove", policy =>
        policy.RequireRole("Staff", "Manager", "Admin", "SuperAdmin"));

    // Customer policy
    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireRole("Customer"));
});

// ============================================================================
// APPLICATION SERVICES (BUSINESS LOGIC)
// ============================================================================

// Shipping Services
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddSingleton<IShippingProvider, FedExProvider>();

// Cart Services (Session-based for both guest and authenticated users)
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IUserStatisticsService, UserStatisticsService>();
// Payment Services
builder.Services.AddScoped<IPaymentService, StripePaymentService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Future services can be added here:
// builder.Services.AddScoped<IOrderService, OrderService>();
// builder.Services.AddScoped<IEmailService, EmailService>();
// builder.Services.AddScoped<INotificationService, NotificationService>();

// ============================================================================
// MVC & RAZOR PAGES
// ============================================================================
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; // THIS LINE CHANGED
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddRazorPages();

// ============================================================================
// BUILD APPLICATION
// ============================================================================
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        // Run migrations
        await context.Database.MigrateAsync();

        // Seed essential data (including Guest User)
        await DbSeeder.SeedEssentialDataAsync(context, userManager, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database");
    }
}
// ============================================================================
// DATABASE INITIALIZATION & SEEDING
// ============================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var startupLogger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        startupLogger.LogInformation("Starting database initialization...");

        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        // Ensure database is created
        var created = await context.Database.EnsureCreatedAsync();
        if (created)
        {
            startupLogger.LogInformation("Database created successfully");
        }
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await DatabaseInitializer.SeedAsync(db);
        // Apply any pending migrations
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            startupLogger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
            await context.Database.MigrateAsync();
            startupLogger.LogInformation("Migrations applied successfully");
        }

        // Seed roles and admin user
        await SeedRolesAndAdminUser(userManager, roleManager, startupLogger);

        startupLogger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "An error occurred during database initialization");

        // In production, you might want to fail fast here
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// ============================================================================
// MIDDLEWARE PIPELINE (ORDER IS CRITICAL!)
// ============================================================================

// 1. Exception Handling (MUST BE FIRST)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}
else
{
    app.UseDeveloperExceptionPage(); // Detailed error page in development
}

// Global error logging middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var errorLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        errorLogger.LogError(ex, "Unhandled exception in request pipeline. Path: {Path}", context.Request.Path);
        throw;
    }
});

// 2. HTTPS Redirection (Production only)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 3. Static Files
app.UseStaticFiles(); // Serve wwwroot files

// 4. Routing
app.UseRouting();

// 5. Session (MUST BE AFTER UseRouting AND BEFORE UseAuthentication)
app.UseSession();

// 6. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 7. Request Logging (Custom middleware)
app.Use(async (context, next) =>
{
    var requestLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var startTime = DateTime.UtcNow;

    requestLogger.LogDebug("Request: {Method} {Path}", context.Request.Method, context.Request.Path);

    await next();

    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
    requestLogger.LogDebug("Response: {StatusCode} in {Duration}ms", context.Response.StatusCode, duration);
});

// ============================================================================
// ENDPOINT ROUTING
// ============================================================================

// Identity Razor Pages (Login, Register, etc.)
app.MapRazorPages();

// Area routes (Admin panel)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Default routes (User-facing pages)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Health check endpoint (optional but recommended)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}));

// ============================================================================
// APPLICATION STARTUP
// ============================================================================
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();

appLogger.LogInformation("==============================================");
appLogger.LogInformation("Starting Hamro Grocery Application");
appLogger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
appLogger.LogInformation("==============================================");

try
{
    app.Run();
}
catch (Exception ex)
{
    appLogger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    appLogger.LogInformation("Application shut down");
}

// ============================================================================
// HELPER METHODS
// ============================================================================

/// <summary>
/// Seeds database with default roles and admin user
/// </summary>
static async Task SeedRolesAndAdminUser(
    UserManager<User> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    ILogger seedLogger)
{
    try
    {
        seedLogger.LogInformation("Starting role and user seeding...");

        // Define all application roles
        string[] roles = { "SuperAdmin", "Admin", "Manager", "Staff", "Customer" };

        // Create roles if they don't exist
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                if (result.Succeeded)
                {
                    seedLogger.LogInformation("✓ Created role: {RoleName}", roleName);
                }
                else
                {
                    seedLogger.LogError("✗ Failed to create role {RoleName}: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // Create default admin user
        var adminEmail = "admin@hamrogrocery.com";
        var adminPassword = "Admin@123456";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                PhoneNumber = "+61 412 345 678",
                PhoneNumberConfirmed = true,
                IsActive = true,
                IsApproved = true,
                Role = UserRole.SuperAdmin,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                seedLogger.LogInformation("==============================================");
                seedLogger.LogInformation("✓ Admin User Created Successfully");
                seedLogger.LogInformation("  Email: {Email}", adminEmail);
                seedLogger.LogInformation("  Password: {Password}", adminPassword);
                seedLogger.LogWarning("⚠ CHANGE DEFAULT PASSWORD IMMEDIATELY!");
                seedLogger.LogInformation("==============================================");
            }
            else
            {
                seedLogger.LogError("✗ Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            seedLogger.LogInformation("Admin user already exists: {Email}", adminEmail);

            // Ensure admin has SuperAdmin role
            if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                seedLogger.LogInformation("✓ Added SuperAdmin role to existing admin user");
            }

            // Ensure admin is active
            if (!adminUser.IsActive)
            {
                adminUser.IsActive = true;
                await userManager.UpdateAsync(adminUser);
                seedLogger.LogInformation("✓ Activated admin user");
            }
        }

        seedLogger.LogInformation("Role and user seeding completed successfully");
    }
    catch (Exception ex)
    {
        seedLogger.LogError(ex, "Error occurred during role and user seeding");
        throw;
    }
}