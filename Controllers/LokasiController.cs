// Controllers/LokasiController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Claims;

// ===== Alias model inti =====
using AparLokasi = AparAppsWebsite.Models.Lokasi;
using AparLokasiListVM = AparAppsWebsite.Models.LokasiListVM;
using AparLokasiFormVM = AparAppsWebsite.Models.LokasiFormVM;
using AparPetugasOption = AparAppsWebsite.Models.PetugasOption;
using AparLokasiFormRequest = AparAppsWebsite.Models.LokasiFormRequest;
using AparPetugasListItemVM = AparAppsWebsite.Models.PetugasListItemVM;

namespace AparWebAdmin.Controllers
{
    [Authorize] // 🔒 Wajib login untuk semua aksi di controller ini
    public class LokasiController : Controller
    {
        private readonly HttpClient _http;
        private readonly ILogger<LokasiController> _log;

        #region Endpoint Paths
        private const string LOKASI_BASE = "api/lokasi"; // Base address di Program.cs untuk ApiClient
        private static string PathList() => $"{LOKASI_BASE}";
        private static string PathCreate() => $"{LOKASI_BASE}";
        private static string PathById(int id) => $"{LOKASI_BASE}/{id}";
        private static string PathWithPetugas(int id) => $"{LOKASI_BASE}/{id}/petugas";
        private static string PathFormMeta() => $"{LOKASI_BASE}/form-meta";
        private static string PathAddPetugas(int lokasiId, int petugasId, bool asPIC)
            => $"{LOKASI_BASE}/{lokasiId}/petugas/{petugasId}?asPIC={(asPIC ? "true" : "false")}";
        private static string PathUnlinkPetugas(int lokasiId, int petugasId)
            => $"{LOKASI_BASE}/{lokasiId}/petugas/{petugasId}";
        #endregion

        public LokasiController(IHttpClientFactory httpFactory, ILogger<LokasiController> log)
        {
            _http = httpFactory.CreateClient("ApiClient");
            _log = log;
        }

        // =========================
        // ===== Role Helpers  =====
        // =========================
        private bool IsAdmin() => User?.IsInRole("AdminWeb") == true;
        private bool IsRescue() => User?.IsInRole("Rescue") == true;
        private string GetUserBadge() => User?.FindFirst("BadgeNumber")?.Value ?? "";

        private bool UserHasAccessToLokasi(AparLokasi lokasi, IEnumerable<PetugasListItemFallback> items)
        {
            if (IsAdmin() || IsRescue()) return true;

            var myBadge = GetUserBadge();
            if (string.IsNullOrWhiteSpace(myBadge)) return false;

            // Akses jika user adalah PIC lokasi
            if (!string.IsNullOrWhiteSpace(lokasi?.PIC_BadgeNumber) &&
                string.Equals(lokasi.PIC_BadgeNumber, myBadge, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Akses jika user terdaftar sebagai petugas di lokasi (berdasarkan BadgeNumber)
            if (items?.Any() == true &&
                items.Any(p => !string.IsNullOrWhiteSpace(p.BadgeNumber) &&
                               p.BadgeNumber.Equals(myBadge, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private static double? ToDouble(decimal? v) => v.HasValue ? (double?)Convert.ToDouble(v.Value) : null;
        private static decimal? ToDecimal(double? v) => v.HasValue ? (decimal?)Convert.ToDecimal(v.Value) : null;

        // ===== Helper: parse angka dengan InvariantCulture (titik sebagai desimal) =====
        private static double? ParseInvariant(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Replace(',', '.'); // antisipasi user ketik koma
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? d
                : (double?)null;
        }

        // ===========================
        // LIST
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 20)
        {
            var all = await GetLokasiAsync() ?? new List<AparLokasi>();

            // 🔐 Filter akses:
            // AdminWeb & Rescue -> semua
            // Lainnya -> hanya lokasi di mana user adalah PIC (berdasarkan klaim BadgeNumber)
            if (!IsAdmin() && !IsRescue())
            {
                var myBadge = GetUserBadge();
                if (!string.IsNullOrWhiteSpace(myBadge))
                {
                    all = all
                        .Where(l => !string.IsNullOrWhiteSpace(l.PIC_BadgeNumber) &&
                                    l.PIC_BadgeNumber.Equals(myBadge, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    all = new List<AparLokasi>();
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qq = q.Trim().ToLowerInvariant();
                all = all.Where(l =>
                        (l.Nama ?? "").ToLower().Contains(qq) ||
                        (l.PIC_Nama ?? "").ToLower().Contains(qq) ||
                        (l.PIC_BadgeNumber ?? "").ToLower().Contains(qq))
                    .ToList();
            }

            var total = all.Count;
            var items = all.OrderBy(l => l.Nama ?? string.Empty)
                           .Skip(Math.Max(0, (page - 1) * pageSize))
                           .Take(pageSize)
                           .ToList();

            ViewBag.Query = q ?? "";
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.SortBy = "Nama";
            ViewBag.SortDir = "ASC";

            return View(new AparLokasiListVM { items = items, page = page, pageSize = pageSize, total = total });
        }

        // ===========================
        // CREATE
        // ===========================
        [HttpGet]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Create()
        {
            var meta = await GetLokasiFormMetaAsync();
            var vm = new AparLokasiFormVM
            {
                PICOptions = meta?.petugasTanpaLokasi ?? new List<AparPetugasOption>()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Create(AparLokasiFormVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Nama))
                ModelState.AddModelError(nameof(vm.Nama), "Nama lokasi wajib diisi.");

            // ⬇️ PENTING: paksa parse invariant dari form agar tidak terjebak culture id-ID (koma)
            var latStr = Request?.Form["lat"].ToString();
            var lonStr = Request?.Form["Longitude"].ToString();
            var latParsed = ParseInvariant(latStr);
            var lonParsed = ParseInvariant(lonStr);
            if (latParsed.HasValue) vm.lat = latParsed;
            if (lonParsed.HasValue) vm.Longitude = lonParsed;

            if (!ModelState.IsValid)
            {
                vm.PICOptions = (await GetLokasiFormMetaAsync())?.petugasTanpaLokasi ?? new();
                return View(vm);
            }

            // 🔐 Normalisasi koordinat (auto-swap + round 6 + clamp di VM)
            var (lat6, lon6) = vm.NormalizeLatLon();

            var payload = new AparLokasiFormRequest
            {
                nama = vm.Nama?.Trim(),
                lat = lat6,
                @long = lon6,
                picPetugasId = vm.PICPetugasId
            };

            var (ok, err, data) = await CreateLokasiAsync(payload);
            if (!ok)
            {
                TempData["Error"] = err ?? "Gagal menyimpan lokasi.";
                vm.PICOptions = (await GetLokasiFormMetaAsync())?.petugasTanpaLokasi ?? new();
                return View(vm);
            }

            // Tambah PIC tambahan (optional)
            if (data?.Id is int lokasiId && lokasiId > 0 && vm.PICMultiIds?.Any() == true)
            {
                var extras = vm.PICMultiIds
                    .Distinct()
                    .Where(id => !vm.PICPetugasId.HasValue || id != vm.PICPetugasId.Value);

                foreach (var petugasId in extras)
                {
                    var (added, addErr) = await AddPetugasToLokasiAsync(lokasiId, petugasId, asPIC: true);
                    if (!added)
                    {
                        TempData["Warning"] = (TempData["Warning"] as string ?? "") +
                                              $"Gagal menambah PIC tambahan (PetugasId={petugasId}): {addErr}. ";
                    }
                }
            }

            TempData["Success"] = "Lokasi berhasil dibuat.";
            return RedirectToAction(nameof(Details), new { id = data?.Id ?? 0 });
        }

        // ===========================
        // DETAILS
        // ===========================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var pack = await GetLokasiWithPetugasAsync(id);
            if (pack is null)
            {
                TempData["Error"] = "Lokasi tidak ditemukan atau API gagal.";
                return RedirectToAction(nameof(Index));
            }

            var (lokasi, items) = pack.Value;

            // 🔐 Enforcement akses per-lokasi
            if (!UserHasAccessToLokasi(lokasi, items))
            {
                TempData["Error"] = "Anda tidak memiliki akses ke lokasi ini.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new AparLokasiFormVM
            {
                Id = lokasi.Id,
                Nama = lokasi.Nama,
                lat = ToDouble(lokasi.lat),
                Longitude = ToDouble(lokasi.@long),
                PICPetugasId = lokasi.PICPetugasId,
                CurrentPetugas = items.Select(x => new AparPetugasListItemVM
                {
                    Id = x.Id,
                    BadgeNumber = x.BadgeNumber,
                    Nama = x.EmployeeNama,
                    Role = x.RoleNama
                }).ToList()
            };

            return View(vm);
        }

        // ===========================
        // EDIT
        // ===========================
        [HttpGet]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Edit(int id)
        {
            var pack = await GetLokasiWithPetugasAsync(id);
            if (pack is null)
            {
                TempData["Error"] = "Lokasi tidak ditemukan.";
                return RedirectToAction(nameof(Index));
            }

            var (lokasi, items) = pack.Value;
            var meta = await GetLokasiFormMetaAsync();

            // Gabungkan: petugas yang sudah di lokasi + kandidat tanpa lokasi
            var options = new List<AparPetugasOption>();
            options.AddRange(items.Select(x => new AparPetugasOption
            {
                Id = x.Id,
                BadgeNumber = x.BadgeNumber ?? "",
                EmployeeNama = x.EmployeeNama ?? "",
                RoleNama = x.RoleNama
            }));
            var existingIds = new HashSet<int>(options.Select(o => o.Id));
            options.AddRange((meta?.petugasTanpaLokasi ?? new List<AparPetugasOption>())
                .Where(o => !existingIds.Contains(o.Id)));

            var vm = new AparLokasiFormVM
            {
                Id = lokasi.Id,
                Nama = lokasi.Nama,
                lat = ToDouble(lokasi.lat),
                Longitude = ToDouble(lokasi.@long),
                PICPetugasId = lokasi.PICPetugasId,
                PICOptions = options.OrderBy(o => o.EmployeeNama).ThenBy(o => o.BadgeNumber).ToList(),
                CurrentPetugas = items.Select(x => new AparPetugasListItemVM
                {
                    Id = x.Id,
                    BadgeNumber = x.BadgeNumber,
                    Nama = x.EmployeeNama,
                    Role = x.RoleNama
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Edit(int id, AparLokasiFormVM vm)
        {
            if (id != vm.Id) return BadRequest();
            if (string.IsNullOrWhiteSpace(vm.Nama))
                ModelState.AddModelError(nameof(vm.Nama), "Nama lokasi wajib diisi.");

            // ⬇️ PENTING: parse invariant dari form setiap submit edit
            var latStr = Request?.Form["lat"].ToString();
            var lonStr = Request?.Form["Longitude"].ToString();
            var latParsed = ParseInvariant(latStr);
            var lonParsed = ParseInvariant(lonStr);
            if (latParsed.HasValue) vm.lat = latParsed;
            if (lonParsed.HasValue) vm.Longitude = lonParsed;

            if (!ModelState.IsValid)
            {
                // Repopulate agar form tetap terisi
                var packInvalid = await GetLokasiWithPetugasAsync(id);
                if (packInvalid is null) return NotFound();
                var (lokasiInvalid, itemsInvalid) = packInvalid.Value;

                var metaInvalid = await GetLokasiFormMetaAsync();
                var optionsInvalid = new List<AparPetugasOption>();
                optionsInvalid.AddRange(itemsInvalid.Select(x => new AparPetugasOption
                {
                    Id = x.Id,
                    BadgeNumber = x.BadgeNumber ?? "",
                    EmployeeNama = x.EmployeeNama ?? "",
                    RoleNama = x.RoleNama
                }));
                var existingIds = new HashSet<int>(optionsInvalid.Select(o => o.Id));
                optionsInvalid.AddRange((metaInvalid?.petugasTanpaLokasi ?? new List<AparPetugasOption>())
                    .Where(o => !existingIds.Contains(o.Id)));
                vm.PICOptions = optionsInvalid.OrderBy(o => o.EmployeeNama).ThenBy(o => o.BadgeNumber).ToList();

                vm.CurrentPetugas = itemsInvalid.Select(x => new AparPetugasListItemVM
                {
                    Id = x.Id,
                    BadgeNumber = x.BadgeNumber,
                    Nama = x.EmployeeNama,
                    Role = x.RoleNama
                }).ToList();

                return View(vm);
            }

            // 🔐 Normalisasi koordinat
            var (lat6, lon6) = vm.NormalizeLatLon();

            var payload = new AparLokasiFormRequest
            {
                nama = vm.Nama?.Trim(),
                lat = lat6,
                @long = lon6,
                picPetugasId = vm.PICPetugasId
            };

            var (ok, err) = await UpdateLokasiAsync(id, payload);
            if (!ok)
            {
                TempData["Error"] = err ?? "Gagal update lokasi.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            TempData["Success"] = "Lokasi berhasil diupdate.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ===========================
        // DELETE
        // ===========================
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Delete(int id)
        {
            var (ok, err) = await DeleteLokasiAsync(id);
            if (ok)
            {
                TempData["Success"] = "Lokasi berhasil dihapus.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = err ?? "Gagal menghapus lokasi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ===========================
        // RELASI PETUGAS (single)
        // ===========================
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> AddPetugas(int lokasiId, int petugasId, bool asPIC = false)
        {
            var (ok, err) = await AddPetugasToLokasiAsync(lokasiId, petugasId, asPIC);
            TempData[ok ? "Success" : "Error"] = ok ? "Petugas ditambahkan." : err ?? "Gagal menambah petugas.";
            return RedirectToAction(nameof(Edit), new { id = lokasiId });
        }

        private async Task<(bool ok, string? err)> UnlinkPetugasFromLokasiAsync(int lokasiId, int petugasId)
        {
            try
            {
                var res = await _http.DeleteAsync(PathUnlinkPetugas(lokasiId, petugasId));
                if (!res.IsSuccessStatusCode)
                {
                    return (false, await SafeReadErrorAsync(res));
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "UnlinkPetugasFromLokasiAsync error");
                return (false, ex.Message);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> UnlinkPetugas(int lokasiId, int petugasId)
        {
            var (ok, err) = await UnlinkPetugasFromLokasiAsync(lokasiId, petugasId);
            TempData[ok ? "Success" : "Error"] = ok ? "Petugas di-unlink." : err ?? "Gagal unlink petugas.";
            return RedirectToAction(nameof(Edit), new { id = lokasiId });
        }

        // ===========================
        // RELASI PETUGAS (batch/multi) — untuk Edit page
        // ===========================
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> AddManyPIC(int lokasiId, List<int> petugasIds)
        {
            if (petugasIds?.Any() != true)
                return RedirectToAction(nameof(Edit), new { id = lokasiId });

            foreach (var pid in petugasIds.Distinct())
            {
                var (ok, err) = await AddPetugasToLokasiAsync(lokasiId, pid, asPIC: true);
                if (!ok)
                {
                    TempData["Warning"] = (TempData["Warning"] as string ?? "") +
                                          $"Gagal menambah sebagai PIC (PetugasId={pid}): {err}. ";
                }
            }
            TempData["Success"] = "PIC tambahan berhasil diproses.";
            return RedirectToAction(nameof(Edit), new { id = lokasiId });
        }

        // =====================================================================
        // ======================= HELPER API CALLS =============================
        // =====================================================================

        private async Task<List<AparLokasi>?> GetLokasiAsync()
        {
            try
            {
                var r = await _http.GetAsync(PathList());
                if (!r.IsSuccessStatusCode)
                {
                    _log.LogWarning("GET {Path} failed: {Code}", PathList(), r.StatusCode);
                    return new List<AparLokasi>();
                }

                var direct = await r.Content.ReadFromJsonAsync<List<AparLokasi>>();
                if (direct != null) return direct;

                var vm = await r.Content.ReadFromJsonAsync<AparLokasiListVM>();
                return vm?.items ?? new List<AparLokasi>();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "GetLokasiAsync error");
                return new List<AparLokasi>();
            }
        }

        private async Task<(bool ok, string? err, AparLokasi? data)> CreateLokasiAsync(AparLokasiFormRequest payload)
        {
            try
            {
                var res = await _http.PostAsJsonAsync(PathCreate(), payload);
                if (!res.IsSuccessStatusCode)
                {
                    var msg = await SafeReadErrorAsync(res);
                    return (false, msg, null);
                }

                var dto = await res.Content.ReadFromJsonAsync<AparLokasi>();
                if (dto != null) return (true, null, dto);

                var json = await res.Content.ReadFromJsonAsync<JsonElement>();
                if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("data", out var dataEl))
                {
                    var dto2 = dataEl.Deserialize<AparLokasi>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return (true, null, dto2);
                }
                return (true, null, null);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "CreateLokasiAsync error");
                return (false, ex.Message, null);
            }
        }

        private async Task<(bool ok, string? err)> UpdateLokasiAsync(int id, AparLokasiFormRequest payload)
        {
            try
            {
                var res = await _http.PutAsJsonAsync(PathById(id), payload);
                if (!res.IsSuccessStatusCode)
                {
                    return (false, await SafeReadErrorAsync(res));
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "UpdateLokasiAsync error");
                return (false, ex.Message);
            }
        }

        private async Task<(bool ok, string? err)> DeleteLokasiAsync(int id)
        {
            try
            {
                var res = await _http.DeleteAsync(PathById(id));
                if (!res.IsSuccessStatusCode)
                {
                    return (false, await SafeReadErrorAsync(res));
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "DeleteLokasiAsync error");
                return (false, ex.Message);
            }
        }

        // ------ Form Meta (untuk dropdown PIC) ------
        private async Task<LokasiFormMetaFallback?> GetLokasiFormMetaAsync()
        {
            try
            {
                var res = await _http.GetAsync(PathFormMeta());
                if (!res.IsSuccessStatusCode)
                {
                    _log.LogWarning("GET {Path} failed: {Code}", PathFormMeta(), res.StatusCode);
                    return new LokasiFormMetaFallback();
                }
                return await res.Content.ReadFromJsonAsync<LokasiFormMetaFallback>()
                       ?? new LokasiFormMetaFallback();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "GetLokasiFormMetaAsync error");
                return new LokasiFormMetaFallback();
            }
        }

        // ------ Relasi Petugas ------
        private async Task<(bool ok, string? err)> AddPetugasToLokasiAsync(int lokasiId, int petugasId, bool asPIC)
        {
            try
            {
                var res = await _http.PostAsync(PathAddPetugas(lokasiId, petugasId, asPIC), content: null);
                if (!res.IsSuccessStatusCode)
                {
                    return (false, await SafeReadErrorAsync(res));
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "AddPetugasToLokasiAsync error");
                return (false, ex.Message);
            }
        }

        private async Task<(AparLokasi lokasi, List<PetugasListItemFallback> items)?> GetLokasiWithPetugasAsync(int id)
        {
            try
            {
                var path = PathWithPetugas(id);
                var res = await _http.GetAsync(path);

                if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var only = await _http.GetAsync(PathById(id));
                    if (!only.IsSuccessStatusCode) return null;
                    var textOnly = await only.Content.ReadAsStringAsync();
                    return ParseLokasiPayload(textOnly);
                }

                if (!res.IsSuccessStatusCode)
                {
                    var err = await res.Content.ReadAsStringAsync();
                    _log.LogWarning("GET {Path} failed {Code}. Body: {Body}", path, res.StatusCode, err);
                    return null;
                }

                var body = await res.Content.ReadAsStringAsync();
                return ParseLokasiPayload(body);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "GetLokasiWithPetugasAsync error untuk id {Id}", id);
                return null;
            }
        }

        private (AparLokasi lokasi, List<PetugasListItemFallback> items)? ParseLokasiPayload(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                JsonElement lokasiEl = default;
                JsonElement itemsEl = default;
                bool hasLokasi = false, hasItems = false;

                // { lokasi:{}, items:[] }
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("lokasi", out lokasiEl)) hasLokasi = true;
                    if (root.TryGetProperty("items", out itemsEl)) hasItems = true;

                    // { data:{ ... } }
                    if (!hasLokasi && root.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.ValueKind == JsonValueKind.Object)
                        {
                            if (dataEl.TryGetProperty("lokasi", out lokasiEl)) hasLokasi = true;
                            if (dataEl.TryGetProperty("items", out itemsEl)) hasItems = true;

                            if (!hasLokasi) { lokasiEl = dataEl; hasLokasi = true; }
                        }
                    }
                }

                // objek lokasi langsung
                if (!hasLokasi) { lokasiEl = root; hasLokasi = true; }

                var lokasi = new AparLokasi();
                if (hasLokasi && lokasiEl.ValueKind == JsonValueKind.Object)
                {
                    lokasi.Id = GetInt(lokasiEl, "Id", "id");
                    lokasi.Nama = GetString(lokasiEl, "Nama", "nama", "Name");

                    var lat = GetDouble(lokasiEl, "lat", "latitude", "Lat", "Latitude");
                    var lng = GetDouble(lokasiEl, "long", "lng", "longitude", "Lon", "Lng", "Longitude");
                    lokasi.lat = ToDecimal(lat);
                    lokasi.@long = ToDecimal(lng);

                    lokasi.PICPetugasId = GetNullableInt(lokasiEl, "PICPetugasId", "picPetugasId");
                    lokasi.PIC_Nama = GetString(lokasiEl, "PIC_Nama", "picNama", "pic_nama", "PICNama");
                    lokasi.PIC_BadgeNumber = GetString(lokasiEl, "PIC_BadgeNumber", "picBadge", "pic_badge", "PICBadgeNumber");
                    lokasi.PIC_Jabatan = GetString(lokasiEl, "PIC_Jabatan", "picJabatan", "pic_jabatan", "PICJabatan");
                }

                var items = new List<PetugasListItemFallback>();
                if (hasItems && itemsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in itemsEl.EnumerateArray())
                    {
                        if (it.ValueKind != JsonValueKind.Object) continue;
                        items.Add(new PetugasListItemFallback
                        {
                            Id = GetInt(it, "Id", "id"),
                            BadgeNumber = GetString(it, "BadgeNumber", "badge", "badgeNumber", "badge_number"),
                            EmployeeNama = GetString(it, "EmployeeNama", "employeeNama", "nama"),
                            RoleNama = GetString(it, "RoleNama", "roleNama", "role")
                        });
                    }
                }

                return (lokasi, items);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "ParseLokasiPayload gagal. Body: {Body}", body);
                try
                {
                    var lokasiFallback = JsonSerializer.Deserialize<AparLokasi>(body, options);
                    if (lokasiFallback != null) return (lokasiFallback, new List<PetugasListItemFallback>());
                }
                catch { }
                return null;
            }
        }

        // --------- JSON helpers ----------
        private static string? GetString(JsonElement obj, params string[] keys)
        {
            foreach (var k in keys)
                if (obj.TryGetProperty(k, out var el) && el.ValueKind == JsonValueKind.String)
                    return el.GetString();
            return null;
        }
        private static int GetInt(JsonElement obj, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (obj.TryGetProperty(k, out var el))
                {
                    if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)) return v;
                    if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s)) return s;
                }
            }
            return 0;
        }
        private static int? GetNullableInt(JsonElement obj, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (obj.TryGetProperty(k, out var el))
                {
                    if (el.ValueKind == JsonValueKind.Null) return null;
                    if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v)) return v;
                    if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s)) return s;
                }
            }
            return null;
        }
        private static double? GetDouble(JsonElement obj, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (obj.TryGetProperty(k, out var el))
                {
                    if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d)) return d;
                    if (el.ValueKind == JsonValueKind.String &&
                        double.TryParse(el.GetString(),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out var ds)) return ds;
                }
            }
            return null;
        }

        private static async Task<string?> SafeReadErrorAsync(HttpResponseMessage res)
        {
            try
            {
                var json = await res.Content.ReadFromJsonAsync<JsonElement>();
                if (json.ValueKind == JsonValueKind.Object)
                {
                    if (json.TryGetProperty("message", out var msg)) return msg.GetString();
                    if (json.TryGetProperty("error", out var err)) return err.GetString();
                }
                return $"HTTP {(int)res.StatusCode} {res.StatusCode}";
            }
            catch
            {
                return $"HTTP {(int)res.StatusCode} {res.StatusCode}";
            }
        }

        // ====================== FALLBACK DTO ======================
        private class PetugasListItemFallback
        {
            public int Id { get; set; }
            public string? BadgeNumber { get; set; }
            public string? EmployeeNama { get; set; }
            public string? RoleNama { get; set; }
        }
        private class LokasiFormMetaFallback
        {
            public List<AparPetugasOption> petugasTanpaLokasi { get; set; } = new();
        }
    }
}
