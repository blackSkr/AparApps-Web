using AparAppsWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Globalization;

namespace AparWebAdmin.Controllers
{
    [Authorize]
    public class MaintenanceController : Controller
    {
        private readonly HttpClient _http;

        public MaintenanceController(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("ApiClient");
        }

        private bool IsAdmin() => User?.IsInRole("AdminWeb") == true;
        private bool IsRescue() => User?.IsInRole("Rescue") == true;
        private string GetUserBadge() => User?.FindFirst("BadgeNumber")?.Value ?? "";

        private static void NormalizeMaintenance(Maintenance m)
        {
            if (string.IsNullOrWhiteSpace(m.BadgeNumber) && !string.IsNullOrWhiteSpace(m.PetugasBadge))
                m.BadgeNumber = m.PetugasBadge;
            m.Kondisi ??= "";
        }

        private static void NormalizeMaintenanceList(List<Maintenance> list)
        {
            foreach (var m in list) NormalizeMaintenance(m);
        }

        public async Task<IActionResult> Index(int? id)
        {
            try
            {
                HttpResponseMessage res = (id == null)
                    ? await _http.GetAsync("api/perawatan/all")
                    : await _http.GetAsync($"api/perawatan/history/{id}");

                if (!res.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Gagal mengambil data maintenance dari server.";
                    return View(new List<Maintenance>());
                }

                var json = await res.Content.ReadAsStringAsync();
                var wrapper = JsonConvert.DeserializeObject<JObject>(json);
                var list = wrapper?["data"]?.ToObject<List<Maintenance>>() ?? new();
                NormalizeMaintenanceList(list);

                if (!IsAdmin() && !IsRescue())
                {
                    var myBadge = GetUserBadge();
                    list = !string.IsNullOrWhiteSpace(myBadge)
                        ? list.Where(x => string.Equals(x.BadgeNumber ?? "", myBadge, StringComparison.OrdinalIgnoreCase)).ToList()
                        : new List<Maintenance>();
                }

                ViewBag.AparId = id;
                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(new List<Maintenance>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var res = await _http.GetAsync($"api/perawatan/details/{id}");
                if (!res.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Gagal load detail maintenance";
                    return RedirectToAction(nameof(Index));
                }

                var json = await res.Content.ReadAsStringAsync();
                var wrapper = JsonConvert.DeserializeObject<JObject>(json);
                var data = wrapper?["data"]?.ToObject<Maintenance>();
                if (data == null)
                {
                    TempData["Error"] = "Data tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                NormalizeMaintenance(data);

                if (!IsAdmin() && !IsRescue())
                {
                    var myBadge = GetUserBadge();
                    var dataBadge = data.BadgeNumber ?? data.PetugasBadge ?? "";
                    if (string.IsNullOrWhiteSpace(myBadge) ||
                        !string.Equals(myBadge, dataBadge, StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["Error"] = "Anda tidak memiliki akses ke detail maintenance ini.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // Map checklist
                var checklistArr = wrapper?["data"]?["checklist"] as JArray ?? new JArray();
                data.Checklist = checklistArr.Select(c => new ChecklistJawaban
                {
                    ChecklistId = (int?)c["ChecklistId"] ?? 0,
                    Jawaban = ((bool?)c["Dicentang"]) ?? false,
                    Keterangan = (string?)c["Keterangan"] ?? "",
                    PertanyaanChecklist = (string?)c["Pertanyaan"] ?? ""
                }).ToList();

                // NEW: map koordinat (hp.* sudah include)
                data.Latitude = (double?)wrapper?["data"]?["Latitude"];
                data.Longitude = (double?)wrapper?["data"]?["Longitude"];

                // Fix foto: gunakan FotoUrl jika ada
                var photosArr = wrapper?["data"]?["photos"] as JArray ?? new JArray();
                data.Photos = photosArr.Select(p => new FotoPemeriksaan
                {
                    // pakai FotoUrl jika tersedia, else FotoPath
                    FotoPath = (string?)(p["FotoUrl"] ?? p["FotoPath"]) ?? "",
                    UploadedAt = (DateTime?)p["UploadedAt"] ?? DateTime.MinValue
                }).ToList();

                return View(data);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Authorize(Roles = "AdminWeb,Rescue")]
        public async Task<IActionResult> Create(int id, string? badge)
        {
            try
            {
                var badgeFromClaim = GetUserBadge();
                var effectiveBadge = !string.IsNullOrWhiteSpace(badgeFromClaim)
                    ? badgeFromClaim
                    : (IsAdmin() ? (badge ?? "") : "");

                if (string.IsNullOrWhiteSpace(effectiveBadge))
                {
                    TempData["Error"] = "Badge pengguna tidak ditemukan. Silakan login ulang.";
                    return RedirectToAction(nameof(Index));
                }

                var res = await _http.GetAsync($"api/peralatan/with-checklist?id={id}&badge={effectiveBadge}");
                if (!res.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Gagal mengambil data apar & checklist";
                    return RedirectToAction(nameof(Index));
                }

                var json = await res.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<Maintenance>(json) ?? new Maintenance();

                data.PeralatanId = data.Id == 0 ? id : (data.PeralatanId == 0 ? id : data.PeralatanId);
                data.BadgeNumber = effectiveBadge;
                data.TanggalPemeriksaan = DateTime.Now;

                try
                {
                    var root = JObject.Parse(json);
                    var arrStr = (string?)root["keperluan_check"];
                    var arr = !string.IsNullOrWhiteSpace(arrStr)
                        ? JsonConvert.DeserializeObject<List<JObject>>(arrStr) ?? new()
                        : new();

                    data.Checklist = arr.Select(x => new ChecklistJawaban
                    {
                        ChecklistId = (int)(x["checklistId"] ?? 0),
                        PertanyaanChecklist = (string?)x["Pertanyaan"] ?? "",
                        Jawaban = true,
                        Alasan = "",
                        Keterangan = ""
                    }).ToList();
                }
                catch { }

                return View(data);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb,Rescue")]
        public async Task<IActionResult> Create(Maintenance model, List<IFormFile> fotos)
        {
            try
            {
                var badgeFromClaim = GetUserBadge();
                if (string.IsNullOrWhiteSpace(badgeFromClaim))
                {
                    ViewBag.Error = "Badge pengguna tidak ditemukan. Silakan login ulang.";
                    return View(model);
                }
                model.BadgeNumber = badgeFromClaim;

                using var multipart = new MultipartFormDataContent();
                multipart.Add(new StringContent(model.PeralatanId.ToString()), "aparId");
                multipart.Add(new StringContent(model.BadgeNumber ?? ""), "badgeNumber");
                multipart.Add(new StringContent(model.TanggalPemeriksaan.ToString("yyyy-MM-dd")), "tanggal");
                multipart.Add(new StringContent(model.IntervalPetugasId?.ToString() ?? ""), "intervalPetugasId");
                multipart.Add(new StringContent(model.Kondisi ?? ""), "kondisi");
                multipart.Add(new StringContent(model.CatatanMasalah ?? ""), "catatanMasalah");
                multipart.Add(new StringContent(model.Rekomendasi ?? ""), "rekomendasi");
                multipart.Add(new StringContent(model.TindakLanjut ?? ""), "tindakLanjut");
                multipart.Add(new StringContent(model.Tekanan?.ToString(CultureInfo.InvariantCulture) ?? ""), "tekanan");
                multipart.Add(new StringContent(model.JumlahMasalah?.ToString() ?? ""), "jumlahMasalah");

                // NEW: kirim lat/long jika ada (BE sudah support)
                if (model.Latitude.HasValue)
                    multipart.Add(new StringContent(model.Latitude.Value.ToString(CultureInfo.InvariantCulture)), "latitude");
                if (model.Longitude.HasValue)
                    multipart.Add(new StringContent(model.Longitude.Value.ToString(CultureInfo.InvariantCulture)), "longitude");

                var checklistJson = JsonConvert.SerializeObject(
                    (model.Checklist ?? new()).Select(c => new
                    {
                        checklistId = c.ChecklistId,
                        condition = c.Jawaban ? "Baik" : "Tidak",
                        alasan = c.Alasan ?? ""
                    })
                );
                multipart.Add(new StringContent(checklistJson, Encoding.UTF8, "application/json"), "checklist");

                foreach (var file in fotos ?? new())
                {
                    if (file?.Length > 0)
                    {
                        var fileContent = new StreamContent(file.OpenReadStream());
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                        multipart.Add(fileContent, "fotos", file.FileName); // kompatibel field lama
                    }
                }

                var res = await _http.PostAsync("api/perawatan/submit", multipart);
                if (!res.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Gagal menyimpan data maintenance";
                    return View(model);
                }

                TempData["Success"] = "Maintenance berhasil disimpan.";
                return RedirectToAction(nameof(Index), new { id = model.PeralatanId });
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(model);
            }
        }
    }
}
