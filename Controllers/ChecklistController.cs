using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AparAppsWebsite.Models;
using System.Text;

namespace AparWebAdmin.Controllers
{
    public class ChecklistController : Controller
    {
        private readonly HttpClient _httpClient;

        public ChecklistController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        // GET: Checklist
        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/checklist");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var checklist = JsonConvert.DeserializeObject<List<Checklist>>(json) ?? new List<Checklist>();
                    return View(checklist);
                }

                ViewBag.Error = $"API returned: {response.StatusCode}";
                return View(new List<Checklist>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(new List<Checklist>());
            }
        }

        // GET: Checklist/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                await LoadJenisPeralatanDropdown();
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading form: {ex.Message}";
                return View();
            }
        }

        // POST: Checklist/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Checklist checklist)
        {
            if (!ModelState.IsValid)
            {
                await LoadJenisPeralatanDropdown();
                return View(checklist);
            }

            try
            {
                var json = JsonConvert.SerializeObject(checklist);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/checklist", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Checklist berhasil ditambahkan";
                    return RedirectToAction(nameof(Index));
                }

                var errorResponse = await response.Content.ReadAsStringAsync();
                var errorObj = JsonConvert.DeserializeObject<dynamic>(errorResponse);
                ViewBag.Error = errorObj?.message ?? "Gagal menyimpan checklist";

                await LoadJenisPeralatanDropdown();
                return View(checklist);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                await LoadJenisPeralatanDropdown();
                return View(checklist);
            }
        }

        // GET: Checklist/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/checklist/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var checklist = JsonConvert.DeserializeObject<Checklist>(json);

                    await LoadJenisPeralatanDropdown();
                    return View(checklist);
                }

                TempData["Error"] = "Checklist tidak ditemukan";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Checklist/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Checklist checklist)
        {
            if (!ModelState.IsValid)
            {
                await LoadJenisPeralatanDropdown();
                return View(checklist);
            }

            try
            {
                var json = JsonConvert.SerializeObject(checklist);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/checklist/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Checklist berhasil diupdate";
                    return RedirectToAction(nameof(Index));
                }

                var errorResponse = await response.Content.ReadAsStringAsync();
                var errorObj = JsonConvert.DeserializeObject<dynamic>(errorResponse);
                ViewBag.Error = errorObj?.message ?? "Gagal update checklist";

                await LoadJenisPeralatanDropdown();
                return View(checklist);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                await LoadJenisPeralatanDropdown();
                return View(checklist);
            }
        }

        // POST: Checklist/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/checklist/{id}");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Checklist berhasil dihapus";
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(errorResponse);
                    TempData["Error"] = errorObj?.message ?? "Gagal menghapus checklist";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Checklist/Bulk
        public async Task<IActionResult> Bulk()
        {
            try
            {
                await LoadJenisPeralatanDropdown();
                return View(new BulkChecklistCreate());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading form: {ex.Message}";
                return View(new BulkChecklistCreate());
            }
        }

        // POST: Checklist/Bulk
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bulk(BulkChecklistCreate model)
        {
            if (!ModelState.IsValid)
            {
                await LoadJenisPeralatanDropdown();
                return View(model);
            }

            try
            {
                var json = JsonConvert.SerializeObject(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/checklist/bulk", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    TempData["Success"] = result?.message?.ToString() ?? "Checklist berhasil ditambahkan";
                    return RedirectToAction(nameof(Index));
                }

                var errorResponse = await response.Content.ReadAsStringAsync();
                var errorObj = JsonConvert.DeserializeObject<dynamic>(errorResponse);
                ViewBag.Error = errorObj?.message ?? "Gagal menyimpan checklist";

                await LoadJenisPeralatanDropdown();
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                await LoadJenisPeralatanDropdown();
                return View(model);
            }
        }

        // GET: Checklist/ByJenis/5
        public async Task<IActionResult> ByJenis(int jenisId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/checklist/jenis/{jenisId}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var checklist = JsonConvert.DeserializeObject<List<Checklist>>(json) ?? new List<Checklist>();

                    ViewBag.JenisNama = checklist.FirstOrDefault()?.JenisNama ?? "Unknown";
                    return View(checklist);
                }

                ViewBag.Error = $"API returned: {response.StatusCode}";
                return View(new List<Checklist>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(new List<Checklist>());
            }
        }

        // Private method untuk load dropdown jenis peralatan
        private async Task LoadJenisPeralatanDropdown()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/jenis-peralatan");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jenisList = JsonConvert.DeserializeObject<List<JenisPeralatan>>(json) ?? new List<JenisPeralatan>();
                    ViewBag.JenisList = jenisList.Select(j => new DropdownItem
                    {
                        Id = j.Id,
                        Nama = j.Nama
                    }).ToList();
                }
                else
                {
                    ViewBag.JenisList = new List<DropdownItem>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading jenis peralatan: {ex.Message}");
                ViewBag.JenisList = new List<DropdownItem>();
            }
        }
    }
}