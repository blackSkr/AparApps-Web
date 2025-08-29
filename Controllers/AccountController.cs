using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using AparAppsWebsite.Models;
using LoginVM = AparAppsWebsite.Models.LoginViewModel; // ⬅️ alias biar tidak bentrok

namespace AparAppsWebsite.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IHttpClientFactory httpFactory, ILogger<AccountController> logger)
        {
            _httpFactory = httpFactory;
            _logger = logger;
        }

        // ❌ HAPUS inner class LoginViewModel di sini (yang sebelumnya ada)
        // public class LoginViewModel {...}  <-- remove

        private sealed class PetugasDto
        {
            public int Id { get; set; }
            public string? BadgeNumber { get; set; }
            public string? RoleNama { get; set; }
            public string? Role { get; set; }
            public int? LokasiId { get; set; }
            public string? EmployeeNama { get; set; }
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
                return Redirect(string.IsNullOrEmpty(returnUrl) ? Url.Content("~/")! : returnUrl);

            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.BadgeNumber))
            {
                ModelState.AddModelError(nameof(vm.BadgeNumber), "BadgeNumber wajib diisi.");
                return View(vm);
            }

            var client = _httpFactory.CreateClient("ApiClient");
            PetugasDto? petugas = null;

            try
            {
                petugas = await FetchPetugasByBadgeAsync(client, vm.BadgeNumber.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error memanggil API petugas");
            }

            if (petugas == null)
            {
                ModelState.AddModelError(nameof(vm.BadgeNumber), "Badge tidak ditemukan.");
                return View(vm);
            }

            var role = petugas.Role ?? petugas.RoleNama ?? "User";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, petugas.Id.ToString()),
                new Claim(ClaimTypes.Name, petugas.EmployeeNama ?? petugas.BadgeNumber ?? "User"),
                new Claim("BadgeNumber", petugas.BadgeNumber ?? string.Empty),
                new Claim(ClaimTypes.Role, role),
            };
            if (petugas.LokasiId.HasValue)
                claims.Add(new Claim("LokasiId", petugas.LokasiId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = vm.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            return Redirect(string.IsNullOrEmpty(vm.ReturnUrl) ? Url.Content("~/")! : vm.ReturnUrl!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        public IActionResult Denied() => View();

        private async Task<PetugasDto?> FetchPetugasByBadgeAsync(HttpClient client, string rawBadge)
        {
            var badge = (rawBadge ?? "").Trim();

            try
            {
                var r1 = await client.GetAsync($"api/petugas/by-badge/{Uri.EscapeDataString(badge)}");
                if (r1.IsSuccessStatusCode)
                {
                    var dto = await r1.Content.ReadFromJsonAsync<PetugasDto>();
                    if (dto != null)
                    {
                        dto.Role ??= dto.RoleNama;
                        return dto;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "by-badge gagal");
            }

            try
            {
                var r2 = await client.GetAsync($"api/petugas?q={Uri.EscapeDataString(badge)}&page=1&pageSize=50");
                if (r2.IsSuccessStatusCode)
                {
                    using var stream = await r2.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);

                    if (doc.RootElement.TryGetProperty("items", out var items) &&
                        items.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in items.EnumerateArray())
                        {
                            var bn = el.TryGetProperty("BadgeNumber", out var b) ? b.GetString() : null;
                            if (string.Equals(bn?.Trim(), badge, StringComparison.OrdinalIgnoreCase))
                            {
                                var dto = new PetugasDto
                                {
                                    Id = el.TryGetProperty("Id", out var idEl) ? idEl.GetInt32() : 0,
                                    BadgeNumber = bn,
                                    RoleNama = el.TryGetProperty("RoleNama", out var rn) ? rn.GetString() : null,
                                    LokasiId = el.TryGetProperty("LokasiId", out var lid) && lid.ValueKind != JsonValueKind.Null
                                              ? lid.GetInt32() : (int?)null,
                                    EmployeeNama = el.TryGetProperty("EmployeeNama", out var en) ? en.GetString() : null
                                };
                                dto.Role ??= dto.RoleNama;
                                return dto;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "list?q= fallback gagal");
            }

            try
            {
                var r3 = await client.GetAsync($"api/petugas/profile/{Uri.EscapeDataString(badge)}");
                if (r3.IsSuccessStatusCode)
                {
                    using var stream = await r3.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);

                    var dto = new PetugasDto
                    {
                        Id = 0,
                        BadgeNumber = doc.RootElement.TryGetProperty("badgeNumber", out var b) ? b.GetString() : badge,
                        EmployeeNama = doc.RootElement.TryGetProperty("nama", out var n) ? n.GetString() : badge,
                        RoleNama = "User",
                        Role = "User",
                        LokasiId = null
                    };
                    return dto;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "profile fallback gagal");
            }

            return null;
        }
    }
}
