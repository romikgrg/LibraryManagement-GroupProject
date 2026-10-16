using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.Services.Import;
using LibraryManagement.Services.Notifications;
using LibraryManagement.Services.Reporting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// QuestPDF is free for this use under its Community licence. Declaring it is a
// requirement of the library and must happen before any document is generated.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Data access
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=library.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// ---------------------------------------------------------------------------
// Authentication and authorisation: staff sign in, and the three roles from the
// brief decide what they can reach.
// ---------------------------------------------------------------------------
builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/Denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ---------------------------------------------------------------------------
// Policy, read from appsettings.json rather than written into the code.
// ---------------------------------------------------------------------------
builder.Services.Configure<LoanRules>(builder.Configuration.GetSection(LoanRules.SectionName));
builder.Services.Configure<NotificationRules>(builder.Configuration.GetSection(NotificationRules.SectionName));
builder.Services.Configure<BranchDeploymentOptions>(builder.Configuration.GetSection(BranchDeploymentOptions.SectionName));

// ---------------------------------------------------------------------------
// Application services. Controllers depend on these interfaces, never on the
// classes behind them, so a replacement is one line here.
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IFinePolicy, DailyRateFinePolicy>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IBorrowerService, BorrowerService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// Group project modules.
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IBranchContext, BranchContext>();

// Both notification transports are registered against the same interface. The
// notification service receives the whole collection and picks one at run time
// by the member's preference, so adding a push channel later means adding a
// class and one line here.
builder.Services.AddScoped<INotificationChannel, SimulatedEmailChannel>();
builder.Services.AddScoped<INotificationChannel, SimulatedSmsChannel>();

// The mock metadata provider the importer queries. Swapping in a real lookup
// against an ISBN service is a different implementation of this one interface.
builder.Services.AddSingleton<IMetadataProvider, MockMetadataProvider>();

// Session keeps the public portal's recent searches and the kiosk's branch
// between requests.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

// The public developer API returns JSON and is documented through OpenAPI.
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Turns a bare 404 or 403 into the styled page rather than an empty response.
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// The root goes to HomeController.Index, which sends each visitor where they
// actually work: the public to the catalogue, and each staff role to its own
// desk. Routing straight to the catalogue instead would mean a signed-in member
// of staff lands on the public search page after every sign in, because the
// Identity login redirects to "~/".
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Create the database and the three staff logins on first run.
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();
