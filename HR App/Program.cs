using HR_App.Models.TopSoft;
//using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Globalization;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("ar")
    };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider { QueryStringKey = "culture" });
});
// Add services to the container.
builder.Services.AddDbContextFactory<TopSoftContext>(options =>
    options.UseSqlServer("Server=192.168.1.208;User ID=sa;Password=P@ssw0rd123;Database=TopSoft;Connect Timeout=7200;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;"));
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<TopSoftContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMvc();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(180);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login"; 
        options.ExpireTimeSpan = TimeSpan.FromMinutes(180);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = int.MaxValue; // Allow unlimited form fields
    options.ValueLengthLimit = int.MaxValue; // Increase value length limit
    options.MultipartBodyLengthLimit = long.MaxValue; // In case of file uploads
});
builder.Services.AddSignalR();
//builder.Services.AddHangfireServer();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
QuestPDF.Settings.License = LicenseType.Community;
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseRequestLocalization();      // ✅ Move this here
app.UseSession();                 // ✅ Session middleware
app.UseAuthentication();         // ✅ Auth
app.UseAuthorization();          // ✅ Authz

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Logout}/{id?}");
});

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Application crashed: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw; // إعادة الرمي حتى ترى الخطأ في ديباجر أو الـ console
}
