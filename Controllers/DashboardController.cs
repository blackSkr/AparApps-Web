using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AparAppsWebsite.Utils; // User.Badge() & User.IsAdminWeb()

public sealed class DashboardSummaryDto
{
    public int Peralatan { get; set; }
    public int Checklist { get; set; }
    public int Lokasi { get; set; }
    public int Petugas { get; set; }
}

[Authorize] // wajib login
public class DashboardController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public DashboardController(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new DashboardViewModel();
        var api = _httpClientFactory.CreateClient("ApiClient");

        try
        {
            DashboardSummaryDto? dto;

            if (User.IsAdminWeb())
            {
                dto = await api.GetFromJsonAsync<DashboardSummaryDto>("api/dashboard/summary", _json, ct);
            }
            else
            {
                var badge = User.Badge() ?? string.Empty;

                // coba endpoint summary by badge
                dto = await api.GetFromJsonAsync<DashboardSummaryDto>(
                    $"api/dashboard/summary?badgeNumber={Uri.EscapeDataString(badge)}",
                    _json, ct
                );

                // fallback kalau endpoint di atas belum ada
                if (dto is null)
                {
                    dto = new DashboardSummaryDto
                    {
                        Peralatan = await SafeCount(api, $"api/peralatan/count?badgeNumber={Uri.EscapeDataString(badge)}", ct)
                                    ?? await SafeListCount(api, $"api/peralatan?badgeNumber={Uri.EscapeDataString(badge)}", ct),
                        Checklist = await SafeCount(api, $"api/checklist/count?badgeNumber={Uri.EscapeDataString(badge)}", ct)
                                    ?? await SafeListCount(api, $"api/checklist?badgeNumber={Uri.EscapeDataString(badge)}", ct),
                        Lokasi = await SafeCount(api, $"api/lokasi/count?badgeNumber={Uri.EscapeDataString(badge)}", ct)
                                    ?? await SafeListCount(api, $"api/lokasi?badgeNumber={Uri.EscapeDataString(badge)}", ct),
                        Petugas = await SafeCount(api, $"api/petugas/count?badgeNumber={Uri.EscapeDataString(badge)}", ct)
                                    ?? await SafeListCount(api, $"api/petugas?badgeNumber={Uri.EscapeDataString(badge)}", ct),
                    };
                }
            }

            if (dto is null) throw new InvalidOperationException("Response kosong.");

            vm.TotalPeralatan = dto.Peralatan;
            vm.TotalChecklist = dto.Checklist;
            vm.TotalLokasi = dto.Lokasi;
            vm.TotalPetugas = dto.Petugas;
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Gagal memuat dashboard: {ex.Message}";
            vm.TotalPeralatan = vm.TotalChecklist = vm.TotalLokasi = vm.TotalPetugas = 0;
        }

        return View("~/Views/Home/Index.cshtml", vm);
    }

    // ===== Helpers =====
    private static async Task<int?> SafeCount(HttpClient api, string url, CancellationToken ct)
    {
        try
        {
            return await api.GetFromJsonAsync<int>(url, ct);
        }
        catch { return null; }
    }

    // Non-generic: parse sebagai JSON mentah dan hitung jika array
    private static async Task<int> SafeListCount(HttpClient api, string url, CancellationToken ct)
    {
        try
        {
            using var resp = await api.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return doc.RootElement.GetArrayLength();

            // Jika server membungkus data pakai { items: [...] }
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("items", out var items) &&
                items.ValueKind == JsonValueKind.Array)
                return items.GetArrayLength();

            return 0;
        }
        catch { return 0; }
    }
}
