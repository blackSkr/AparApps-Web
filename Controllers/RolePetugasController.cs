using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AparAppsWebsite.Models;

namespace AparWebAdmin.Controllers
{
    public class RolePetugasController : Controller
    {
        private readonly HttpClient _http;

        public RolePetugasController(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("ApiClient");
        }

        // ===== helpers =====

        /// <summary>
        /// Normalisasi key JSON dari BE agar kompatibel dengan model:
        /// - NamaInterval -> IntervalNama
        /// - IntervalBulan -> Bulan
        /// Berlaku untuk payload paged dan single.
        /// </summary>
        private static string NormalizeIntervalKeys(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;

            try
            {
                var token = JToken.Parse(json);

                void FixOne(JObject obj)
                {
                    // IntervalNama
                    if (obj["IntervalNama"] == null && obj["NamaInterval"] != null)
                        obj["IntervalNama"] = obj["NamaInterval"];

                    // Bulan
                    if (obj["Bulan"] == null && obj["IntervalBulan"] != null)
                        obj["Bulan"] = obj["IntervalBulan"];
                }

                if (token is JObject root)
                {
                    // PagedResult: items/items/Items
                    var items = root["items"] ?? root["Items"];
                    if (items is JArray arr)
                    {
                        foreach (var it in arr.OfType<JObject>())
                            FixOne(it);
                    }
                    else
                    {
                        // Single object
                        FixOne(root);
                    }

                    return root.ToString(Formatting.None);
                }

                return json;
            }
            catch
            {
                // kalau gagal parse, balikin apa adanya
                return json;
            }
        }

        private async Task LoadIntervalDropdownAsync(int? selected = null)
        {
            try
            {
                var res = await _http.GetAsync("api/interval-petugas");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    // BE: [{ Id, NamaInterval, Bulan, ... }]
                    var raw = JsonConvert.DeserializeObject<List<dynamic>>(json) ?? new();
                    var list = new List<DropdownItem>();
                    foreach (var i in raw)
                    {
                        int id = (int)i.Id;
                        int? bulan = (int?)i.Bulan;
                        string nama = (string)i.NamaInterval;
                        list.Add(new DropdownItem
                        {
                            Id = id,
                            Nama = bulan.HasValue ? $"{nama} ({bulan} bulan)" : nama
                        });
                    }
                    ViewBag.IntervalList = list;
                    ViewBag.IntervalSelected = selected;
                    return;
                }
            }
            catch { }

            ViewBag.IntervalList = new List<DropdownItem>();
            ViewBag.IntervalSelected = selected;
        }

        private static StringContent JsonContent(object o) =>
            new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");

        // ===== Index (list + optional search) =====
        public async Task<IActionResult> Index(string? q = null, int page = 1, int pageSize = 25)
        {
            try
            {
                var url = $"api/role-petugas?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(q)) url += $"&q={Uri.EscapeDataString(q)}";

                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Gagal mengambil data role petugas";
                    return View(new PagedResult<RolePetugas>());
                }

                var raw = await res.Content.ReadAsStringAsync();
                var json = NormalizeIntervalKeys(raw);

                var data = JsonConvert.DeserializeObject<PagedResult<RolePetugas>>(json)
                           ?? new PagedResult<RolePetugas>();

                ViewBag.Query = q;
                await LoadIntervalDropdownAsync(); // untuk filter dropdown di view
                return View(data);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new PagedResult<RolePetugas>());
            }
        }

        // ===== Details =====
        public async Task<IActionResult> Details(int id)
        {
            var res = await _http.GetAsync($"api/role-petugas/{id}");
            if (!res.IsSuccessStatusCode)
            {
                TempData["Error"] = "Role petugas tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var raw = await res.Content.ReadAsStringAsync();
            var json = NormalizeIntervalKeys(raw);

            var item = JsonConvert.DeserializeObject<RolePetugas>(json) ?? new RolePetugas();
            return View(item);
        }

        // ===== Create =====
        public async Task<IActionResult> Create()
        {
            await LoadIntervalDropdownAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RolePetugas model)
        {
            if (!ModelState.IsValid)
            {
                await LoadIntervalDropdownAsync(model.IntervalPetugasId);
                return View(model);
            }

            var payload = new
            {
                NamaRole = model.NamaRole,
                IntervalPetugasId = model.IntervalPetugasId,
                Deskripsi = model.Deskripsi,
                IsActive = model.IsActive
            };

            var res = await _http.PostAsync("api/role-petugas", JsonContent(payload));
            if (!res.IsSuccessStatusCode)
            {
                ViewBag.Error = await res.Content.ReadAsStringAsync();
                await LoadIntervalDropdownAsync(model.IntervalPetugasId);
                return View(model);
            }

            TempData["Success"] = "Role petugas berhasil ditambahkan";
            return RedirectToAction(nameof(Index));
        }

        // ===== Edit =====
        public async Task<IActionResult> Edit(int id)
        {
            var res = await _http.GetAsync($"api/role-petugas/{id}");
            if (!res.IsSuccessStatusCode)
            {
                TempData["Error"] = "Role petugas tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }

            var raw = await res.Content.ReadAsStringAsync();
            var json = NormalizeIntervalKeys(raw);

            var item = JsonConvert.DeserializeObject<RolePetugas>(json) ?? new RolePetugas();

            await LoadIntervalDropdownAsync(item.IntervalPetugasId);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RolePetugas model)
        {
            if (!ModelState.IsValid)
            {
                await LoadIntervalDropdownAsync(model.IntervalPetugasId);
                return View(model);
            }

            var payload = new
            {
                NamaRole = model.NamaRole,
                IntervalPetugasId = model.IntervalPetugasId,
                Deskripsi = model.Deskripsi,
                IsActive = model.IsActive
            };

            var res = await _http.PutAsync($"api/role-petugas/{id}", JsonContent(payload));
            if (!res.IsSuccessStatusCode)
            {
                ViewBag.Error = await res.Content.ReadAsStringAsync();
                await LoadIntervalDropdownAsync(model.IntervalPetugasId);
                return View(model);
            }

            TempData["Success"] = "Role petugas berhasil diupdate";
            return RedirectToAction(nameof(Index));
        }

        // ===== Delete =====
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var res = await _http.DeleteAsync($"api/role-petugas/{id}");
                if (res.IsSuccessStatusCode)
                    TempData["Success"] = "Role petugas berhasil dihapus";
                else
                    TempData["Error"] = await res.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
