using Microsoft.EntityFrameworkCore;
using Volunteer_website.Models;
using Volunteer_website.ViewModels;
using Volunteer_website.Services;
using Volunteer_website.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OfficeOpenXml; // Add EPPlus namespace

var builder = WebApplication.CreateBuilder(args);

// Set EPPlus license (required for EPPlus 8 to avoid LicenseContextPropertyObsoleteException)
ExcelPackage.License.SetNonCommercialPersonal("Your_Name"); // Replace with your name for noncommercial use
// For commercial use, use: ExcelPackage.License.SetCommercial("Your_License_Key_Here");
// For noncommercial organization, use: ExcelPackage.License.SetNonCommercialOrganization("Your_Organization_Name");

// Thêm dịch vụ vào container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Đăng ký DbContext với SQL Server
builder.Services.AddDbContext<VolunteerManagementContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("VolunteerDB"));
});

// Cấu hình AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// Đăng ký IHttpClientFactory
builder.Services.AddHttpClient();

// Cấu hình Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

// Admin Cookie (bỏ comment nếu cần)
//.AddCookie("Admin", options =>
//{
//    options.LoginPath = "/Account/Login";
//    options.LogoutPath = "/Admin/HomeAdmin/Logout";
//    options.AccessDeniedPath = "/Account/AccessDenied";
//    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
//    options.SlidingExpiration = true;
//});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Volunteer", policy => policy.RequireRole("0"));
    options.AddPolicy("Org", policy => policy.RequireRole("1"));
    options.AddPolicy("Admin", policy => policy.RequireRole("2"));
});

// Cấu hình API Behavior
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Cấu hình Localization
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("vi-VN")
    };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Cấu hình Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký VnPay Service
builder.Services.AddScoped<IVnPayService, VnPayService>();

// Đăng ký EmailService
builder.Services.AddScoped<Volunteer_website.Services.EmailService>();



// Cấu hình logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

var app = builder.Build();

// Cấu hình Middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Cấu hình Request Localization
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

// Định nghĩa Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Debugging Route Info
app.Use(async (context, next) =>
{
    var routeValues = context.GetRouteData();
    Console.WriteLine($"Route Debug: {string.Join(", ", routeValues.Values.Select(kv => $"{kv.Key}={kv.Value}"))}");
    await next();
}); 

app.Run();