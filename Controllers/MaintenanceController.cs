// Controllers/MaintenanceController.cs
using AparAppsWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace AparWebAdmin.Controllers
{
    [Authorize] // 🔒 Wajib login
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

        // Index (global) atau per APAR (?id=xxx)
        // - AdminWeb/Rescue: lihat semua
        // - User biasa: difilter ke miliknya (berdasarkan BadgeNumber)
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

                // Null-safety
                foreach (var m in list)
                    m.Kondisi ??= "";

                // 🔐 Akses: jika bukan Admin/Rescue → filter by badge milik user
                if (!IsAdmin() && !IsRescue())
                {
                    var myBadge = GetUserBadge();
                    if (!string.IsNullOrWhiteSpace(myBadge))
                        list = list.Where(x => string.Equals(x.BadgeNumber, myBadge, StringComparison.OrdinalIgnoreCase)).ToList();
                    else
                        list = new List<Maintenance>();
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

        // Detail pemeriksaan
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

                // 🔐 Akses: Admin/Rescue boleh; selain itu hanya jika BadgeNumber sama
                if (!IsAdmin() && !IsRescue())
                {
                    var myBadge = GetUserBadge();
                    if (string.IsNullOrWhiteSpace(myBadge) ||
                        !string.Equals(myBadge, data.BadgeNumber ?? "", StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["Error"] = "Anda tidak memiliki akses ke detail maintenance ini.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // Map checklist
                var checklistJson = wrapper["data"]?["checklist"]?.ToObject<List<JObject>>() ?? new();
                var checklist = checklistJson.Select(c => new ChecklistJawaban
                {
                    ChecklistId = (int)(c["ChecklistId"] ?? 0),
                    Jawaban = (bool)(c["Dicentang"] ?? false),
                    Keterangan = (string?)c["Keterangan"] ?? "",
                    PertanyaanChecklist = (string?)c["Pertanyaan"] ?? ""
                }).ToList();
                data.Checklist = checklist;

                // Map photos
                data.Photos = wrapper["data"]?["photos"]?.ToObject<List<FotoPemeriksaan>>() ?? new();

                return View(data);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET create → ambil detail APAR + checklist via /api/peralatan/with-checklist?id=&badge=
        [HttpGet]
        [Authorize(Roles = "AdminWeb,Rescue")]
        public async Task<IActionResult> Create(int id, string? badge)
        {
            try
            {
                // 🔐 Ambil badge dari klaim; fallback ke query hanya jika klaim kosong & user Admin
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

                // Set properti form
                data.PeralatanId = data.Id == 0 ? id : (data.PeralatanId == 0 ? id : data.PeralatanId);
                data.BadgeNumber = effectiveBadge;
                data.TanggalPemeriksaan = DateTime.Now;

                // Build checklist default jika tersedia keperluan_check
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
                        Jawaban = true, // default "Baik"
                        Alasan = "",
                        Keterangan = ""
                    }).ToList();
                }
                catch { /* fallback jika gagal parse */ }

                return View(data);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST create → kirim multipart (field + foto) ke /api/perawatan/submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb,Rescue")]
        public async Task<IActionResult> Create(Maintenance model, List<IFormFile> fotos)
        {
            try
            {
                // 🔐 Paksa BadgeNumber dari klaim agar tidak bisa disuntik dari form
                var badgeFromClaim = GetUserBadge();
                if (string.IsNullOrWhiteSpace(badgeFromClaim))
                {
                    ViewBag.Error = "Badge pengguna tidak ditemukan. Silakan login ulang.";
                    return View(model);
                }

                model.BadgeNumber = badgeFromClaim;

                // Bangun multipart form
                using var multipart = new MultipartFormDataContent();

                multipart.Add(new StringContent(model.PeralatanId.ToString()), "aparId");
                multipart.Add(new StringContent(model.BadgeNumber ?? ""), "badgeNumber");
                multipart.Add(new StringContent(model.TanggalPemeriksaan.ToString("yyyy-MM-dd")), "tanggal");
                multipart.Add(new StringContent(model.IntervalPetugasId?.ToString() ?? ""), "intervalPetugasId");
                multipart.Add(new StringContent(model.Kondisi ?? ""), "kondisi");
                multipart.Add(new StringContent(model.CatatanMasalah ?? ""), "catatanMasalah");
                multipart.Add(new StringContent(model.Rekomendasi ?? ""), "rekomendasi");
                multipart.Add(new StringContent(model.TindakLanjut ?? ""), "tindakLanjut");
                multipart.Add(new StringContent(model.Tekanan?.ToString() ?? ""), "tekanan");
                multipart.Add(new StringContent(model.JumlahMasalah?.ToString() ?? ""), "jumlahMasalah");

                // Checklist → {checklistId, condition: "Baik"/"Tidak", alasan}
                var checklistJson = JsonConvert.SerializeObject(
                    (model.Checklist ?? new()).Select(c => new
                    {
                        checklistId = c.ChecklistId,
                        condition = c.Jawaban ? "Baik" : "Tidak",
                        alasan = c.Alasan ?? ""
                    })
                );
                multipart.Add(new StringContent(checklistJson, Encoding.UTF8, "application/json"), "checklist");

                // Foto
                foreach (var file in fotos ?? new())
                {
                    if (file?.Length > 0)
                    {
                        var fileContent = new StreamContent(file.OpenReadStream());
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                        multipart.Add(fileContent, "fotos", file.FileName);
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
