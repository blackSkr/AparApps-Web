using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AparAppsWebsite.Models;
using System.Text;

namespace AparWebAdmin.Controllers
{
    public class IntervalPetugasController : Controller
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOpt = new() { PropertyNameCaseInsensitive = true };

        public IntervalPetugasController(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("ApiClient");
        }

        // GET: /IntervalPetugas
        public async Task<IActionResult> Index()
        {
            try
            {
                var res = await _http.GetAsync("api/interval-petugas");
                if (!res.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Gagal mengambil data interval petugas dari API.";
                    return View(new List<IntervalPetugas>());
                }

                var json = await res.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<IntervalPetugas>>(json, _jsonOpt) ?? new();
                return View(items);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(new List<IntervalPetugas>());
            }
        }

        // GET: /IntervalPetugas/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var res = await _http.GetAsync($"api/interval-petugas/{id}");
                if (!res.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Interval petugas tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                var json = await res.Content.ReadAsStringAsync();
                var item = JsonSerializer.Deserialize<IntervalPetugas>(json, _jsonOpt);
                return View(item);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /IntervalPetugas/Create
        public IActionResult Create() => View();

        // POST: /IntervalPetugas/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IntervalPetugas model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    NamaInterval = model.NamaInterval,
                    Bulan = model.Bulan ?? 0
                });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var res = await _http.PostAsync("api/interval-petugas", content);
                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Interval petugas berhasil ditambahkan.";
                    return RedirectToAction(nameof(Index));
                }

                var err = await res.Content.ReadAsStringAsync();
                ViewBag.Error = TryParseApiMessage(err) ?? "Gagal menambah interval petugas.";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(model);
            }
        }

        // GET: /IntervalPetugas/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var res = await _http.GetAsync($"api/interval-petugas/{id}");
                if (!res.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Interval petugas tidak ditemukan.";
                    return RedirectToAction(nameof(Index));
                }

                var json = await res.Content.ReadAsStringAsync();
                var item = JsonSerializer.Deserialize<IntervalPetugas>(json, _jsonOpt);
                return View(item);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /IntervalPetugas/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IntervalPetugas model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    Id = model.Id,
                    NamaInterval = model.NamaInterval,
                    Bulan = model.Bulan ?? 0
                });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                var res = await _http.PutAsync($"api/interval-petugas/{id}", content);
                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Interval petugas berhasil diupdate.";
                    return RedirectToAction(nameof(Index));
                }

                var err = await res.Content.ReadAsStringAsync();
                ViewBag.Error = TryParseApiMessage(err) ?? "Gagal update interval petugas.";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(model);
            }
        }

        // POST: /IntervalPetugas/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var res = await _http.DeleteAsync($"api/interval-petugas/{id}");
                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Interval petugas berhasil dihapus.";
                }
                else
                {
                    var err = await res.Content.ReadAsStringAsync();
                    TempData["Error"] = TryParseApiMessage(err) ?? "Gagal menghapus interval petugas.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        private static string? TryParseApiMessage(string json)
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return dict != null && dict.TryGetValue("message", out var msg) ? msg?.ToString() : null;
            }
            catch { return null; }
        }
    }
}
