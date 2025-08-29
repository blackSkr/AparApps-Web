using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AparAppsWebsite.Utils; // User.Badge() & User.IsAdminWeb()
using System.Globalization;

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
                // Ringkasan global (admin)
                dto = await api.GetFromJsonAsync<DashboardSummaryDto>("api/dashboard/summary", _json, ct);
            }
            else
            {
                var badge = User.Badge() ?? string.Empty;

                // 1) Ringkasan count personal (fallback via ?badgeNumber=)
                dto = await api.GetFromJsonAsync<DashboardSummaryDto>(
                    $"api/dashboard/summary?badgeNumber={Uri.EscapeDataString(badge)}",
                    _json, ct
                );
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

                // 2) Ambil lokasi petugas (profile)
                string lokasiPetugas = "";
                try
                {
                    using var respP = await api.GetAsync($"api/petugas/profile/{Uri.EscapeDataString(badge)}", ct);
                    if (respP.IsSuccessStatusCode)
                    {
                        await using var s = await respP.Content.ReadAsStreamAsync(ct);
                        using var jp = await JsonDocument.ParseAsync(s, cancellationToken: ct);
                        lokasiPetugas = GetStringAlt(jp.RootElement, new[] { "lokasi", "Lokasi", "LokasiNama" }) ?? "";
                    }
                }
                catch { /* ignore */ }

                // 3) Hitung total inspeksi bulan ini (dari /api/perawatan/all → filter badge & bulan)
                int totalInspeksiBulanIni = 0;
                try
                {
                    using var respAll = await api.GetAsync("api/perawatan/all", ct);
                    if (respAll.IsSuccessStatusCode)
                    {
                        await using var s = await respAll.Content.ReadAsStreamAsync(ct);
                        using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);

                        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                            doc.RootElement.TryGetProperty("data", out var arr) &&
                            arr.ValueKind == JsonValueKind.Array)
                        {
                            var now = DateTime.Now;
                            foreach (var el in arr.EnumerateArray())
                            {
                                var bn = (GetStringAlt(el, new[] { "BadgeNumber", "PetugasBadge" }) ?? "").Trim();
                                if (!bn.Equals(badge.Trim(), StringComparison.OrdinalIgnoreCase)) continue;

                                var dtStr = GetStringAlt(el, new[] { "TanggalPemeriksaan", "Tanggal", "TanggalInspeksi" });
                                if (DateTime.TryParse(dtStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var tgl))
                                {
                                    if (tgl.Month == now.Month && tgl.Year == now.Year)
                                        totalInspeksiBulanIni++;
                                }
                            }
                        }
                    }
                }
                catch { /* ignore */ }

                // 4) Distribusi jenis alat di lokasi petugas
                //    Ambil daftar peralatan yang terkait badge (tanpa ubah BE), lalu grup by Jenis
                var jenisCounts = new List<(string Jenis, int Count)>();
                try
                {
                    using var respEquip = await api.GetAsync($"api/peralatan?badgeNumber={Uri.EscapeDataString(badge)}", ct);
                    if (respEquip.IsSuccessStatusCode)
                    {
                        await using var s = await respEquip.Content.ReadAsStreamAsync(ct);
                        using var je = await JsonDocument.ParseAsync(s, cancellationToken: ct);

                        // Data bisa berupa array langsung atau { items: [...] }
                        JsonElement listEl = default;
                        if (je.RootElement.ValueKind == JsonValueKind.Array)
                            listEl = je.RootElement;
                        else if (je.RootElement.ValueKind == JsonValueKind.Object &&
                                 je.RootElement.TryGetProperty("items", out var it) &&
                                 it.ValueKind == JsonValueKind.Array)
                            listEl = it;

                        if (listEl.ValueKind == JsonValueKind.Array)
                        {
                            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                            foreach (var item in listEl.EnumerateArray())
                            {
                                // Cek lokasi sesuai lokasiPetugas jika tersedia di item
                                var lokasiItem = GetStringAlt(item, new[] { "LokasiNama", "Lokasi", "NamaLokasi" }) ?? "";
                                if (!string.IsNullOrWhiteSpace(lokasiPetugas) &&
                                    !string.IsNullOrWhiteSpace(lokasiItem) &&
                                    !lokasiItem.Equals(lokasiPetugas, StringComparison.OrdinalIgnoreCase))
                                {
                                    // skip bila peralatan bukan di lokasi petugas
                                    continue;
                                }

                                var jenis = GetStringAlt(item, new[] { "JenisNama", "Jenis", "NamaJenis", "JenisPeralatanNama" }) ?? "Tidak diketahui";
                                if (!map.ContainsKey(jenis)) map[jenis] = 0;
                                map[jenis]++;
                            }

                            jenisCounts = map
                                .OrderByDescending(kv => kv.Value)
                                .ThenBy(kv => kv.Key)
                                .Select(kv => (kv.Key, kv.Value))
                                .ToList();
                        }
                    }
                }
                catch { /* ignore */ }

                // Oper ke view via ViewBag agar tidak perlu ubah class DashboardViewModel
                ViewBag.MyBadge = badge;
                ViewBag.LokasiPetugas = lokasiPetugas;
                ViewBag.TotalInspeksiBulanIni = totalInspeksiBulanIni;
                ViewBag.JenisCounts = jenisCounts;
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
        try { return await api.GetFromJsonAsync<int>(url, ct); }
        catch { return null; }
    }

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

            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("items", out var items) &&
                items.ValueKind == JsonValueKind.Array)
                return items.GetArrayLength();

            return 0;
        }
        catch { return 0; }
    }

    private static string? GetStringAlt(JsonElement el, string[] keys)
    {
        foreach (var k in keys)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        }
        return null;
    }
}
