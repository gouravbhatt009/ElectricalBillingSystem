using ElectricalBilling.Data;
using ElectricalBilling.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// QuestPDF's free Community license — required once at startup for any
// non-commercial / small-business use (this app qualifies). See
// https://www.questpdf.com/license/ for the current eligibility terms.
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------
// 1. Database (SQLite via EF Core)
// -------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Set it in appsettings.json or User Secrets.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// -------------------------------------------------------------------
// 2. MVC
// -------------------------------------------------------------------
builder.Services.AddControllersWithViews();

// -------------------------------------------------------------------
// 3. Cookie Authentication
// -------------------------------------------------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.Name = "ElectricalBilling.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

// -------------------------------------------------------------------
// 4. Application Services (business logic lives here, not in controllers)
// -------------------------------------------------------------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBusinessSettingsService, BusinessSettingsService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IServiceMasterService, ServiceMasterService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

// -------------------------------------------------------------------
// Forwarded Headers for Cloud Reverse Proxies (Render SSL Support)
// -------------------------------------------------------------------
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// -------------------------------------------------------------------
// 5. Apply migrations + seed initial admin account (first run only)
// -------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider, app.Configuration);
}

// -------------------------------------------------------------------
// 6. Middleware pipeline
// -------------------------------------------------------------------
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
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();