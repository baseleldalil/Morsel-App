using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Data.Seeders;
using WhatsApp.Shared.Models;
using WhatsAppAdmin.Repositories;
using WhatsAppAdmin.Services;


var builder = WebApplication.CreateBuilder(args);

// Fix for Npgsql TimeSpan overflow with PostgreSQL timestamps
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<SaaSDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedEmail = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<SaaSDbContext>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;

    // Lock down user creation to admin only
    options.Stores.MaxLengthForKeys = 128;
    options.User.RequireUniqueEmail = true;
});

// Add authorization with SuperAdmin bypass
builder.Services.AddAuthorization();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, WhatsAppAdmin.Authorization.SuperAdminAuthorizationHandler>();

// Add HTTP Context Accessor (required for repositories to access current user)
builder.Services.AddHttpContextAccessor();

// Add MVC
builder.Services.AddControllersWithViews();

// Register admin-specific repositories
builder.Services.AddScoped<ICampaignTemplateRepository, CampaignTemplateRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
// Note: IPermissionRepository disabled - Permission model not in SaaSDbContext
// builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();


// Register admin-specific services (keeping existing ones)
builder.Services.AddScoped<ICampaignTemplateService, CampaignTemplateService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
// Note: IPermissionService disabled - Permission model not in SaaSDbContext
// builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<ITimingControlService, TimingControlService>();
builder.Services.AddHttpClient<IWhatsAppApiService, WhatsAppApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Block registration routes
app.MapGet("/Identity/Account/Register", (HttpContext context) =>
{
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

app.MapPost("/Identity/Account/Register", (HttpContext context) =>
{
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();

// Seed all data on startup
using (var scope = app.Services.CreateScope())
{
    var saasContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Apply any pending migrations
    //await saasContext.Database.MigrateAsync();

    // Seed admin roles and users
    if (app.Environment.IsDevelopment())
    {
        Console.WriteLine("🌱 Starting admin user seeding...\n");

        // Use the AdminUserSeeder to seed roles and admin users
        await AdminUserSeeder.SeedAsync(userManager, roleManager);

        // Display summary of seeded admin users
        await AdminUserSeeder.DisplaySummaryAsync(userManager);

        Console.WriteLine("✅ Admin user seeding completed!\n");
    }
}

app.Run();