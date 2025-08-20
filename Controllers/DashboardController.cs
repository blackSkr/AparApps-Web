using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

public sealed class DashboardSummaryDto
{
    public int Peralatan { get; set; }
    public int Checklist { get; set; }
    public int Lokasi { get; set; }
    public int Petugas { get; set; }
}

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
        var url = "api/dashboard/summary";

        try
        {
            var dto = await api.GetFromJsonAsync<DashboardSummaryDto>(url, _json, ct);
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

        // Tetap pakai view Home/Index.cshtml
        return View("~/Views/Home/Index.cshtml", vm);
    }
}
