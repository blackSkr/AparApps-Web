// Controllers/EmployeeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AparAppsWebsite.Models;
using System.Text;

namespace AparWebAdmin.Controllers
{
    [Authorize(Roles = "AdminWeb")] // 🔒 Hanya AdminWeb yang boleh akses modul Employee
    public class EmployeeController : Controller
    {
        private readonly HttpClient _http;

        public EmployeeController(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("ApiClient");
        }

        // GET: /Employee
        // query: q, page, pageSize, sortBy (Id|Nama|BadgeNumber|Divisi|Departemen), sortDir (ASC|DESC)
        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10, string sortBy = "Id", string sortDir = "ASC")
        {
            try
            {
                var url = $"api/employee?page={page}&pageSize={pageSize}&sortBy={sortBy}&sortDir={sortDir}";
                if (!string.IsNullOrWhiteSpace(q))
                    url += $"&q={Uri.EscapeDataString(q)}";

                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode)
                {
                    ViewBag.Error = $"API returned: {res.StatusCode}";
                    return View(new EmployeePagedResponse());
                }

                var json = await res.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<EmployeePagedResponse>(json) ?? new EmployeePagedResponse();

                ViewBag.Query = q ?? "";
                ViewBag.SortBy = sortBy;
                ViewBag.SortDir = sortDir;
                ViewBag.Page = data.page;
                ViewBag.PageSize = data.pageSize;
                ViewBag.Total = data.total;

                return View(data);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(new EmployeePagedResponse());
            }
        }

        // GET: /Employee/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var res = await _http.GetAsync($"api/employee/{id}");
                if (!res.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Employee tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }
                var json = await res.Content.ReadAsStringAsync();
                var emp = JsonConvert.DeserializeObject<Employee>(json);
                return View(emp);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Employee/Create
        public IActionResult Create() => View();

        // POST: /Employee/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // BE menerima berbagai casing; kirim camelCase standar
                var payload = new
                {
                    nama = model.Nama,
                    badgeNumber = model.BadgeNumber,
                    Divisi = model.Divisi,
                    Departemen = model.Departemen
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var res = await _http.PostAsync("api/employee", content);
                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Employee berhasil ditambahkan";
                    return RedirectToAction(nameof(Index));
                }

                var err = await res.Content.ReadAsStringAsync();
                dynamic? obj = null;
                try { obj = JsonConvert.DeserializeObject<dynamic>(err); } catch { }
                ViewBag.Error = obj?.message ?? "Gagal menyimpan employee";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(model);
            }
        }

        // GET: /Employee/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var res = await _http.GetAsync($"api/employee/{id}");
                if (!res.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Employee tidak ditemukan";
                    return RedirectToAction(nameof(Index));
                }
                var json = await res.Content.ReadAsStringAsync();
                var emp = JsonConvert.DeserializeObject<Employee>(json);
                return View(emp);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Employee/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var payload = new
                {
                    nama = model.Nama,
                    badgeNumber = model.BadgeNumber,
                    Divisi = model.Divisi,
                    Departemen = model.Departemen
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var res = await _http.PutAsync($"api/employee/{id}", content);
                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Employee berhasil diupdate";
                    return RedirectToAction(nameof(Index));
                }

                var err = await res.Content.ReadAsStringAsync();
                dynamic? obj = null;
                try { obj = JsonConvert.DeserializeObject<dynamic>(err); } catch { }
                ViewBag.Error = obj?.message ?? "Gagal update employee";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(model);
            }
        }

        // POST: /Employee/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var res = await _http.DeleteAsync($"api/employee/{id}");
                if (res.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Employee berhasil dihapus";
                }
                else
                {
                    var err = await res.Content.ReadAsStringAsync();
                    dynamic? obj = null;
                    try { obj = JsonConvert.DeserializeObject<dynamic>(err); } catch { }
                    TempData["Error"] = obj?.message ?? "Gagal menghapus employee";
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
