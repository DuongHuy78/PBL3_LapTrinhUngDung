using Microsoft.EntityFrameworkCore;
using Volunteer_website.Models;
using Volunteer_website.ViewModels;
using Volunteer_website.Services;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure Entity Framework Core
builder.Services.AddDbContext<VolunteerManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VolunteerDB")));

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// Configure Authentication & Authorization
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
    options.AddPolicy("Admin", policy=> policy.RequireRole("2"));
});

builder.Services.AddAuthorization();

// Configure API Behavior
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Configure Localization
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("en-US") };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian session tồn tại
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
//Connect Vnpay
builder.Services.AddScoped<IVnPayService, VnPayService>();

builder.Services.AddDbContext<VolunteerManagementContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyDatabase"));
});

var app = builder.Build();

// Configure Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Đảm bảo phục vụ các tệp tĩnh từ wwwroot

app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Debugging Route Info
app.Use(async (context, next) =>
{
    var routeValues = context.GetRouteData();
    Console.WriteLine($"Route Debug: {string.Join(", ", routeValues.Values.Select(kv => $"{kv.Key}={kv.Value}"))}");
    await next();
});

app.Run();
