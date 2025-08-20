// Controllers/PetugasController.cs
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using AparAppsWebsite.Models;
using System.Linq;

namespace AparWebAdmin.Controllers
{
    public class PetugasController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly JsonSerializerOptions _json;

        public PetugasController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            _json = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        private class Paged<T>
        {
            public int page { get; set; }
            public int pageSize { get; set; }
            public int total { get; set; }
            public List<T> items { get; set; } = new();
        }
        private class RoleFromRoleApi
        {
            public int Id { get; set; }
            public string NamaRole { get; set; } = "";
            public int? IntervalPetugasId { get; set; }
            public string? NamaInterval { get; set; }
            public int? Bulan { get; set; }
        }
        private class LokasiFromApi
        {
            public int Id { get; set; }
            public string Nama { get; set; } = "";
        }

        // ===== Tambahan: DTO untuk lookup employee =====
        private class EmployeeDto
        {
            public int Id { get; set; }
            public string? BadgeNumber { get; set; }
            public string? Nama { get; set; }
            public string? Divisi { get; set; }
            public string? Departemen { get; set; }
            public string? Status { get; set; }
        }

        private static List<RoleItem> MapRolesForDropdown(IEnumerable<RoleRawItem> src)
        {
            var list = new List<RoleItem>();
            foreach (var r in src ?? Enumerable.Empty<RoleRawItem>())
            {
                var it = !string.IsNullOrWhiteSpace(r.NamaInterval)
                    ? (r.Bulan.HasValue ? $"{r.NamaInterval} ({r.Bulan} bln)" : r.NamaInterval)
                    : null;
                list.Add(new RoleItem
                {
                    Id = r.Id,
                    Display = it != null ? $"{r.NamaRole} — {it}" : r.NamaRole
                });
            }
            return list;
        }

        private async Task<PetugasFormMeta?> GetFormMeta()
        {
            var client = _clientFactory.CreateClient("ApiClient");

            // utama
            try
            {
                using var resp = await client.GetAsync("/api/petugas/form-meta");
                if (resp.IsSuccessStatusCode)
                {
                    var meta = JsonSerializer.Deserialize<PetugasFormMeta>(
                        await resp.Content.ReadAsStringAsync(), _json);
                    if (meta?.roles?.Count > 0) return meta;
                }
            }
            catch { }

            // fallback
            var fallback = new PetugasFormMeta();

            try
            {
                using var rRoles = await client.GetAsync("/api/role-petugas?page=1&pageSize=500");
                if (rRoles.IsSuccessStatusCode)
                {
                    var txt = await rRoles.Content.ReadAsStringAsync();
                    var paged = JsonSerializer.Deserialize<Paged<RoleFromRoleApi>>(txt, _json);
                    foreach (var r in paged?.items ?? new())
                        fallback.roles.Add(new RoleRawItem
                        {
                            Id = r.Id,
                            NamaRole = r.NamaRole,
                            IntervalPetugasId = r.IntervalPetugasId,
                            NamaInterval = r.NamaInterval,
                            Bulan = r.Bulan
                        });
                }
            }
            catch { }

            try
            {
                using var rLok = await client.GetAsync("/api/lokasi");
                if (rLok.IsSuccessStatusCode)
                {
                    var txt = await rLok.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<LokasiFromApi>>(txt, _json) ?? new();
                    fallback.lokasi = list.Select(l => new LokasiItem { Id = l.Id, Nama = l.Nama }).ToList();
                }
            }
            catch { }

            if ((fallback.roles?.Count ?? 0) == 0 && (fallback.lokasi?.Count ?? 0) == 0) return null;
            return fallback;
        }

        // INDEX
        public async Task<IActionResult> Index(string? q, int? roleId, int? lokasiId, int page = 1, int pageSize = 20, string sortBy = "BadgeNumber", string sortDir = "ASC")
        {
            var client = _clientFactory.CreateClient("ApiClient");
            var url = new StringBuilder("/api/petugas?");
            if (!string.IsNullOrWhiteSpace(q)) url.Append($"q={Uri.EscapeDataString(q)}&");
            if (roleId.HasValue) url.Append($"roleId={roleId}&");
            if (lokasiId.HasValue) url.Append($"lokasiId={lokasiId}&");
            url.Append($"page={page}&pageSize={pageSize}&sortBy={sortBy}&sortDir={sortDir}");

            using var resp = await client.GetAsync(url.ToString());
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = "Gagal mengambil data petugas.";
                return View(new PetugasListResponse());
            }

            var data = JsonSerializer.Deserialize<PetugasListResponse>(await resp.Content.ReadAsStringAsync(), _json)
                       ?? new PetugasListResponse();

            var meta = await GetFormMeta();
            if (meta == null) TempData["Error"] = "Gagal memuat form meta.";
            ViewBag.Roles = MapRolesForDropdown(meta?.roles ?? new());
            ViewBag.Lokasi = meta?.lokasi ?? new List<LokasiItem>();

            ViewBag.Query = q;
            ViewBag.RoleId = roleId;
            ViewBag.LokasiId = lokasiId;
            ViewBag.Page = data.page;
            ViewBag.PageSize = data.pageSize;
            ViewBag.Total = data.total;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;

            return View(data);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var client = _clientFactory.CreateClient("ApiClient");
            using var resp = await client.GetAsync($"/api/petugas/{id}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
            resp.EnsureSuccessStatusCode();

            var model = JsonSerializer.Deserialize<Petugas>(await resp.Content.ReadAsStringAsync(), _json);
            return View(model);
        }

        // ===== NEW: EMPLOYEE LOOKUP (tanpa ubah BE; fleksibel beberapa bentuk respons) =====
        [HttpGet]
        public async Task<IActionResult> EmployeeLookup(string? q, int limit = 20, int page = 1)
        {
            var client = _clientFactory.CreateClient("ApiClient");

            async Task<(bool ok, string body)> TryGet(string url)
            {
                try
                {
                    using var r = await client.GetAsync(url);
                    if (!r.IsSuccessStatusCode) return (false, $"{(int)r.StatusCode}:{url}");
                    return (true, await r.Content.ReadAsStringAsync());
                }
                catch (Exception ex) { return (false, $"{ex.GetType().Name}:{url}"); }
            }

            // Coba beberapa pola endpoint yang umum
            var tries = new[]
            {
                "/api/employee",
                "/api/employee?page=1&pageSize=500",
                "/api/employee/list"
            };

            string? payload = null;
            foreach (var u in tries)
            {
                var (ok, body) = await TryGet(u);
                if (ok) { payload = body; break; }
            }
            if (payload == null) return Json(Array.Empty<object>());

            List<EmployeeDto> list = new();
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                {
                    list = JsonSerializer.Deserialize<List<EmployeeDto>>(payload, _json) ?? new();
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                        list = JsonSerializer.Deserialize<List<EmployeeDto>>(items.GetRawText(), _json) ?? new();
                    else if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                        list = JsonSerializer.Deserialize<List<EmployeeDto>>(data.GetRawText(), _json) ?? new();
                    else if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array)
                        list = JsonSerializer.Deserialize<List<EmployeeDto>>(result.GetRawText(), _json) ?? new();
                    else
                    {
                        try { list = JsonSerializer.Deserialize<List<EmployeeDto>>(payload, _json) ?? new(); }
                        catch { }
                    }
                }
            }
            catch { list = new(); }

            IEnumerable<EmployeeDto> filtered = list;
            if (!string.IsNullOrWhiteSpace(q))
            {
                filtered = list.Where(e =>
                    (e.BadgeNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (e.Nama ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (e.Divisi ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (e.Departemen ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                );
            }

            var results = filtered
                .Take(limit)
                .Select(e => new
                {
                    id = e.Id,
                    badgeNumber = e.BadgeNumber,
                    nama = e.Nama,
                    text = $"{(e.BadgeNumber ?? "-")} — {e.Nama}"
                           + ((string.IsNullOrWhiteSpace(e.Divisi) && string.IsNullOrWhiteSpace(e.Departemen))
                               ? "" : $" ({e.Divisi}/{e.Departemen})")
                });

            return Json(results);
        }

        // CREATE GET — ambil role meta
        public async Task<IActionResult> Create()
        {
            var meta = await GetFormMeta();
            if (meta == null)
            {
                TempData["Error"] = "Gagal memuat form meta.";
                ViewBag.RoleMetaJson = "[]";
                return View(new PetugasCreateVm { Roles = new() });
            }

            ViewBag.RoleMetaJson = JsonSerializer.Serialize(meta.roles, _json);
            var vm = new PetugasCreateVm { Roles = MapRolesForDropdown(meta.roles) };
            return View(vm);
        }

        // CREATE POST — kirim employeeId (BE set EmployeeId+BadgeNumber otomatis)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PetugasCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                var meta = await GetFormMeta();
                ViewBag.RoleMetaJson = JsonSerializer.Serialize(meta?.roles ?? new(), _json);
                vm.Roles = MapRolesForDropdown(meta?.roles ?? new());
                return View(vm);
            }

            var client = _clientFactory.CreateClient("ApiClient");
            var payload = new
            {
                employeeId = vm.EmployeeId,      // kunci
                rolePetugasId = vm.RolePetugasId // lokasiId opsional
            };
            var content = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");

            using var resp = await client.PostAsync("/api/petugas", content);
            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                var reason = ExtractApiMessage(msg) ?? "Gagal menambah petugas.";
                TempData["Error"] = reason;

                var meta = await GetFormMeta();
                ViewBag.RoleMetaJson = JsonSerializer.Serialize(meta?.roles ?? new(), _json);
                vm.Roles = MapRolesForDropdown(meta?.roles ?? new());
                return View(vm);
            }

            TempData["Success"] = "Petugas berhasil ditambahkan.";
            return RedirectToAction(nameof(Index));
        }

        // EDIT GET — (struktur lama dipertahankan)
        // EDIT GET — tanpa akses data.EmployeeId (karena Petugas tidak punya properti itu)
public async Task<IActionResult> Edit(int id)
{
    var client = _clientFactory.CreateClient("ApiClient");
    using var resp = await client.GetAsync($"/api/petugas/{id}");
    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
    resp.EnsureSuccessStatusCode();

    var data = JsonSerializer.Deserialize<Petugas>(await resp.Content.ReadAsStringAsync(), _json);
    var meta = await GetFormMeta();

    var vm = new PetugasEditVm
    {
        Id = data!.Id,
        // TIDAK ADA: EmployeeId = data.EmployeeId,
        EmployeeNama = data.EmployeeNama,   // tampil read-only jika perlu
        BadgeNumber = data.BadgeNumber,     // tampil read-only jika perlu
        RolePetugasId = data.RolePetugasId,
        LokasiId = data.LokasiId,
        Roles = MapRolesForDropdown(meta?.roles ?? new()),
        Lokasi = meta?.lokasi ?? new()
    };

    return View(vm);
}

        // EDIT POST (PATCH role/lokasi — struktur lama dipertahankan)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PetugasEditVm vm)
        {
            if (id != vm.Id) return BadRequest();

            var client = _clientFactory.CreateClient("ApiClient");
            var patchPayload = new { rolePetugasId = vm.RolePetugasId, lokasiId = vm.LokasiId };
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/petugas/{id}")
            {
                Content = new StringContent(JsonSerializer.Serialize(patchPayload, _json), Encoding.UTF8, "application/json")
            };

            using var resp = await client.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Error"] = ExtractApiMessage(await resp.Content.ReadAsStringAsync()) ?? "Gagal mengubah petugas.";
                var meta = await GetFormMeta();
                vm.Roles = MapRolesForDropdown(meta?.roles ?? new());
                vm.Lokasi = meta?.lokasi ?? new();
                return View(vm);
            }

            TempData["Success"] = "Perubahan petugas tersimpan.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Petugas/Delete/5
public async Task<IActionResult> Delete(int id)
{
    var client = _clientFactory.CreateClient("ApiClient");
    var resp = await client.GetAsync($"/api/petugas/{id}");
    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
    resp.EnsureSuccessStatusCode();

    var data = JsonSerializer.Deserialize<Petugas>(
        await resp.Content.ReadAsStringAsync(), _json
    );

    return View(data);
}

// POST: Petugas/Delete/5
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var client = _clientFactory.CreateClient("ApiClient");
    var resp = await client.DeleteAsync($"/api/petugas/{id}");
    resp.EnsureSuccessStatusCode();

    TempData["success"] = "Petugas berhasil dihapus.";
    return RedirectToAction(nameof(Index));
}


        private static string? ExtractApiMessage(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    return m.GetString();
            }
            catch { }
            return null;
        }
    }
}
