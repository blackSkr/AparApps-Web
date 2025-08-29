// Program.cs
using System.Net.Http.Headers;
using dotenv.net; // (dotnet add package dotenv.net)

var builder = WebApplication.CreateBuilder(args);

// 1) Load .env lebih awal agar masuk sebagai Environment Variables
DotEnv.Fluent()
     .WithTrimValues()
     .WithProbeForEnv()
     .Load();

// 2) MVC
builder.Services.AddControllersWithViews();

// 3) Ambil BaseUrl dengan urutan prioritas:
//    ENV (.env) -> appsettings.json -> fallback lokal
var apiBaseUrl =
    builder.Configuration["Api:BaseUrl"] ??
    Environment.GetEnvironmentVariable("Api__BaseUrl") ??
    "http://localhost:3000/";

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

var app = builder.Build();

// ===== Pipeline (tetap) =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
