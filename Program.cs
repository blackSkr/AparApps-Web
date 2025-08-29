// Program.cs
using System.Net.Http.Headers;
using dotenv.net; // (dotnet add package dotenv.net)
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1) Load .env lebih awal agar masuk sebagai Environment Variables
DotEnv.Fluent()
     .WithTrimValues()
     .WithProbeForEnv() // cari .env di root proyek
     .Load();

// 2) MVC
builder.Services.AddControllersWithViews();

// 3) Ambil BaseUrl dengan urutan prioritas: ENV (.env) -> appsettings.json -> fallback lokal
var apiBaseUrl =
    Environment.GetEnvironmentVariable("Api__BaseUrl") ??      // dari .env (Api__BaseUrl=http://.../)
    builder.Configuration["Api:BaseUrl"] ??                    // dari appsettings.json
    "http://localhost:3000/";                                  // fallback lokal

// 4) Named HttpClient (tetap sama seperti sebelumnya)
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    // Kalau butuh auth header:
    // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "YOUR_TOKEN");
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5)); // opsional

// (Opsional) untuk HTTPS dev self-signed:
// builder.Services.AddHttpClient("ApiClient")
//     .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
//     {
//         ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
//     });

// 5) Cookie Authentication + Authorization (untuk login badge + role AdminWeb)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";        // halaman login
        options.LogoutPath = "/Account/Logout";      // halaman logout
        options.AccessDeniedPath = "/Account/Denied";// halaman akses ditolak
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    // Policy opsional; setara dengan [Authorize(Roles="AdminWeb")]
    options.AddPolicy("AdminWebOnly", p => p.RequireRole("AdminWeb"));
});

var app = builder.Build();

// ===== Pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: auth dulu baru authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
