using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AparAppsWebsite.Models;
using System.Text;

namespace AparWebAdmin.Controllers
{
    public class JenisPeralatanController : Controller
    {
        private readonly HttpClient _http;

        public JenisPeralatanController(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("ApiClient");
        }

        // GET: JenisPeralatan
        public async Task<IActionResult> Index()
        {
            try
            {
                var res = await _http.GetAsync("api/jenis-peralatan");
                if (!res.IsSuccessStatusCode)
                {
                    ViewBag.Error = $"API returned: {res.StatusCode}";
                    return View(new List<JenisPeralatan>());
                }

                var json = await res.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<JenisPeralatan>>(json) ?? new();
                return View(list);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(new List<JenisPeralatan>());
            }
        }
        // GET: JenisPeralatan/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var res = await _http.GetAsync($"api/jenis-peralatan/{id}");
                if (!res.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Jenis peralatan tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                var json = await res.Content.ReadAsStringAsync();

                // 1) Coba direct: { "id":1, "nama":"...", "intervalPemeriksaanBulan":12 }
                var direct = JsonConvert.DeserializeObject<JenisPeralatan>(json);

                // 2) Kalau null, coba wrapped: { "data": { ... } }
                if (direct == null || (direct.Id == 0 && string.IsNullOrWhiteSpace(direct.Nama)))
                {
                    var root = JsonConvert.DeserializeObject<dynamic>(json);
                    if (root != null && root.data != null)
                    {
                        direct = JsonConvert.DeserializeObject<JenisPeralatan>(root.data.ToString()!)!;
                    }
                }

                if (direct == null || direct.Id == 0)
                {
                    TempData["Error"] = "Data jenis peralatan tidak valid dari API";
                    return RedirectToAction(nameof(Index));
                }

                return View(direct);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: JenisPeralatan/Create
        public IActionResult Create() => View();

        // POST: JenisPeralatan/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JenisPeralatan model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var payload = JsonConvert.SerializeObject(model);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var res = await _http.PostAsync("api/jenis-peralatan", content);

                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Jenis peralatan berhasil ditambahkan";
                    return RedirectToAction(nameof(Index));
                }

                var err = await res.Content.ReadAsStringAsync();
                dynamic? obj = JsonConvert.DeserializeObject(err);
                ViewBag.Error = obj?.message ?? "Gagal menyimpan jenis peralatan";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(model);
            }
        }

        // GET: JenisPeralatan/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var res = await _http.GetAsync($"api/jenis-peralatan/{id}");
                if (!res.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Jenis peralatan tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }

                var json = await res.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<JenisPeralatan>(json);
                return View(data);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: JenisPeralatan/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JenisPeralatan model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var payload = JsonConvert.SerializeObject(model);
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var res = await _http.PutAsync($"api/jenis-peralatan/{id}", content);

                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Jenis peralatan berhasil diupdate";
                    return RedirectToAction(nameof(Index));
                }

                var err = await res.Content.ReadAsStringAsync();
                dynamic? obj = JsonConvert.DeserializeObject(err);
                ViewBag.Error = obj?.message ?? "Gagal update jenis peralatan";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(model);
            }
        }

        // POST: JenisPeralatan/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var res = await _http.DeleteAsync($"api/jenis-peralatan/{id}");
                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Jenis peralatan berhasil dihapus";
                }
                else
                {
                    var err = await res.Content.ReadAsStringAsync();
                    dynamic? obj = JsonConvert.DeserializeObject(err);
                    TempData["Error"] = obj?.message ?? "Gagal menghapus jenis peralatan";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
