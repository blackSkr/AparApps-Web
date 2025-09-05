using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AparAppsWebsite.Models;
using System.Text;

namespace AparWebAdmin.Controllers
{
    [Authorize]
    [Route("Admin/[controller]")] // ➜ /Admin/Peralatan/...
    public class PeralatanController : Controller
    {
        private readonly HttpClient _httpClient;

        public PeralatanController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        private void SetApiBase() => ViewBag.ApiBase = _httpClient.BaseAddress?.ToString()?.TrimEnd('/');

        // =========================
        // LIST (server-side filter)
        // =========================
        [HttpGet("")]
        [Authorize(Roles = "AdminWeb,Rescue")]
        public async Task<IActionResult> Index(
            string? q,
            int? jenisId,
            int? lokasiId,
            int page = 1,
            int pageSize = 10,
            string sortBy = "Kode",
            string sortDir = "ASC")
        {
            try
            {
                SetApiBase();

                var response = await _httpClient.GetAsync("api/peralatan/admin");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = await ReadApiErrorAsync(response, "Gagal memuat data peralatan");
                    return View(new List<Peralatan>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var all = JsonConvert.DeserializeObject<List<Peralatan>>(json) ?? new List<Peralatan>();

                // normalisasi ringan (hindari null/whitespace)
                foreach (var x in all)
                {
                    x.Kode = x.Kode?.Trim();
                    x.JenisNama = x.JenisNama?.Trim();
                    x.LokasiNama = x.LokasiNama?.Trim();
                    x.DetailNamaLokasi = x.DetailNamaLokasi?.Trim(); // ✅ penting utk filter tampilan
                    x.Status = x.Status?.Trim();
                    x.Keterangan = x.Keterangan?.Trim();
                }

                // hanya yang BELUM expired
                bool NotExpired(Peralatan p)
                {
                    var byStatus = !string.Equals(p.Status ?? "", "Exp", StringComparison.OrdinalIgnoreCase);
                    var byDate = !p.Exp_Date.HasValue || p.Exp_Date.Value.Date >= DateTime.Today;
                    return byStatus && byDate;
                }
                var data = all.Where(NotExpired).ToList();

                // search/filter
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var key = q.Trim().ToLowerInvariant();
                    data = data.Where(x =>
                        (x.Kode ?? "").ToLower().Contains(key) ||
                        (x.LokasiNama ?? "").ToLower().Contains(key) ||
                        (x.DetailNamaLokasi ?? "").ToLower().Contains(key) || // ✅ ikut dicari
                        (x.JenisNama ?? "").ToLower().Contains(key) ||
                        (x.Spesifikasi ?? "").ToLower().Contains(key) ||
                        (x.Keterangan ?? "").ToLower().Contains(key)
                    ).ToList();
                }
                if (jenisId is > 0) data = data.Where(x => x.JenisId == jenisId).ToList();
                if (lokasiId is > 0) data = data.Where(x => x.LokasiId == lokasiId).ToList();

                // sorting
                bool asc = !string.Equals(sortDir, "DESC", StringComparison.OrdinalIgnoreCase);
                data = (sortBy?.ToLowerInvariant()) switch
                {
                    "lokasi" => (asc ? data.OrderBy(x => x.LokasiNama).ThenBy(x => x.DetailNamaLokasi)
                                     : data.OrderByDescending(x => x.LokasiNama).ThenByDescending(x => x.DetailNamaLokasi)).ToList(), // ✅
                    "jenis" => (asc ? data.OrderBy(x => x.JenisNama) : data.OrderByDescending(x => x.JenisNama)).ToList(),
                    "exp_date" => (asc ? data.OrderBy(x => x.Exp_Date) : data.OrderByDescending(x => x.Exp_Date)).ToList(),
                    "status" => (asc ? data.OrderBy(x => x.Status) : data.OrderByDescending(x => x.Status)).ToList(),
                    _ => (asc ? data.OrderBy(x => x.Kode) : data.OrderByDescending(x => x.Kode)).ToList()
                };

                // paging
                page = Math.Max(1, page);
                pageSize = pageSize <= 0 ? 10 : pageSize;
                var total = data.Count;
                var totalPages = Math.Max(1, (int)Math.Ceiling(total / Math.Max(1.0, pageSize)));
                page = Math.Min(page, totalPages);
                var paged = data.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // dropdowns
                await LoadFilterDropdownDataForIndex(all);

                // viewbag ui
                ViewBag.Query = q ?? "";
                ViewBag.JenisId = jenisId;
                ViewBag.LokasiId = lokasiId;
                ViewBag.SortBy = sortBy;
                ViewBag.SortDir = asc ? "ASC" : "DESC";
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.Total = total;

                return View(paged);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(new List<Peralatan>());
            }
        }

        // ==========================
        // LIST EXPIRED (server-side)
        // ==========================
        [HttpGet("Expired")]
        [Authorize(Roles = "AdminWeb,Rescue")]
        public async Task<IActionResult> Expired(
            string? q,
            int? jenisId,
            int? lokasiId,
            int page = 1,
            int pageSize = 10,
            string sortBy = "Kode",
            string sortDir = "ASC")
        {
            try
            {
                SetApiBase();

                // Pakai filter dari API Node (lebih hemat), fallback nanti kita filter lagi di sini
                var resp = await _httpClient.GetAsync("api/peralatan/admin?onlyExp=1");
                if (!resp.IsSuccessStatusCode)
                {
                    ViewBag.Error = await ReadApiErrorAsync(resp, "Gagal memuat data peralatan Exp");
                    return View("Expired", new List<Peralatan>());
                }

                var json = await resp.Content.ReadAsStringAsync();
                var all = JsonConvert.DeserializeObject<List<Peralatan>>(json) ?? new List<Peralatan>();

                // normalisasi
                foreach (var x in all)
                {
                    x.Kode = x.Kode?.Trim();
                    x.JenisNama = x.JenisNama?.Trim();
                    x.LokasiNama = x.LokasiNama?.Trim();
                    x.DetailNamaLokasi = x.DetailNamaLokasi?.Trim(); // ✅
                    x.Status = x.Status?.Trim();
                    x.Keterangan = x.Keterangan?.Trim();
                }

                // backup filter di sisi .NET (kalau Node belum diupdate)
                bool IsExpired(Peralatan p)
                {
                    bool byStatus = string.Equals(p.Status ?? "", "Exp", StringComparison.OrdinalIgnoreCase);
                    bool byDate = p.Exp_Date.HasValue && p.Exp_Date.Value.Date < DateTime.Today;
                    return byStatus || byDate;
                }
                var data = all.Where(IsExpired).ToList();

                // search + filter
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var key = q.Trim().ToLowerInvariant();
                    data = data.Where(x =>
                        (x.Kode ?? "").ToLower().Contains(key) ||
                        (x.LokasiNama ?? "").ToLower().Contains(key) ||
                        (x.DetailNamaLokasi ?? "").ToLower().Contains(key) || // ✅
                        (x.JenisNama ?? "").ToLower().Contains(key) ||
                        (x.Spesifikasi ?? "").ToLower().Contains(key) ||
                        (x.Keterangan ?? "").ToLower().Contains(key) ||
                        (x.Status ?? "").ToLower().Contains(key)
                    ).ToList();
                }
                if (jenisId is > 0) data = data.Where(x => x.JenisId == jenisId).ToList();
                if (lokasiId is > 0) data = data.Where(x => x.LokasiId == lokasiId).ToList();

                // sort
                bool asc = !string.Equals(sortDir, "DESC", StringComparison.OrdinalIgnoreCase);
                data = (sortBy?.ToLowerInvariant()) switch
                {
                    "lokasi" => (asc ? data.OrderBy(x => x.LokasiNama).ThenBy(x => x.DetailNamaLokasi)
                                     : data.OrderByDescending(x => x.LokasiNama).ThenByDescending(x => x.DetailNamaLokasi)).ToList(), // ✅
                    "jenis" => (asc ? data.OrderBy(x => x.JenisNama) : data.OrderByDescending(x => x.JenisNama)).ToList(),
                    "status" => (asc ? data.OrderBy(x => x.Status) : data.OrderByDescending(x => x.Status)).ToList(),
                    "exp_date" => (asc ? data.OrderBy(x => x.Exp_Date) : data.OrderByDescending(x => x.Exp_Date)).ToList(),
                    "updated" => (asc ? data.OrderBy(x => x.Tanggal_Update_Alat) : data.OrderByDescending(x => x.Tanggal_Update_Alat)).ToList(),
                    _ => (asc ? data.OrderBy(x => x.Kode) : data.OrderByDescending(x => x.Kode)).ToList()
                };

                // paging
                page = Math.Max(1, page);
                pageSize = pageSize <= 0 ? 10 : pageSize;
                var total = data.Count;
                var totalPages = Math.Max(1, (int)Math.Ceiling(total / Math.Max(1.0, pageSize)));
                page = Math.Min(page, totalPages);
                var paged = data.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // dropdown
                await LoadFilterDropdownDataForIndex(all);

                // viewbag
                ViewBag.Query = q ?? "";
                ViewBag.JenisId = jenisId;
                ViewBag.LokasiId = lokasiId;
                ViewBag.SortBy = sortBy;
                ViewBag.SortDir = asc ? "ASC" : "DESC";
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.Total = total;

                return View("Expired", paged);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View("Expired", new List<Peralatan>());
            }
        }

        // =============
        // CREATE (GET)
        // =============
        [HttpGet("Create")]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Create()
        {
            SetApiBase();
            await LoadDropdownData();
            return View();
        }

        // =============
        // CREATE (POST)
        // =============
        [HttpPost("Create"), ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Create(Peralatan peralatan, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                SetApiBase();
                await LoadDropdownData();
                return View(peralatan);
            }

            try
            {
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(peralatan.Kode ?? ""), "Kode");
                form.Add(new StringContent(peralatan.JenisId.ToString()), "JenisId");
                form.Add(new StringContent(peralatan.LokasiId.ToString()), "LokasiId");
                form.Add(new StringContent(peralatan.Spesifikasi ?? ""), "Spesifikasi");

                if (peralatan.Exp_Date.HasValue)
                    form.Add(new StringContent(peralatan.Exp_Date.Value.ToString("yyyy-MM-dd")), "exp_date");

                if (files != null)
                {
                    foreach (var f in files.Where(f => f?.Length > 0))
                    {
                        var sc = new StreamContent(f.OpenReadStream());
                        sc.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(f.ContentType ?? "application/octet-stream");
                        form.Add(sc, "files", f.FileName);
                    }
                }

                var resp = await _httpClient.PostAsync("api/peralatan/admin", form);
                if (!resp.IsSuccessStatusCode)
                {
                    ViewBag.Error = await ReadApiErrorAsync(resp, "Gagal menyimpan peralatan");
                    SetApiBase(); await LoadDropdownData();
                    return View(peralatan);
                }

                TempData["Success"] = "Peralatan berhasil ditambahkan";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                SetApiBase(); await LoadDropdownData();
                return View(peralatan);
            }
        }

        // ===========
        // EDIT (GET)
        // ===========
        [HttpGet("Edit/{id:int}")]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                SetApiBase();
                var resp = await _httpClient.GetAsync($"api/peralatan/admin/{id}");
                if (!resp.IsSuccessStatusCode)
                {
                    TempData["Error"] = await ReadApiErrorAsync(resp, "Peralatan tidak ditemukan");
                    return RedirectToAction(nameof(Index));
                }

                var json = await resp.Content.ReadAsStringAsync();
                var peralatan = JsonConvert.DeserializeObject<Peralatan>(json) ?? new Peralatan();
                await LoadDropdownData();
                return View(peralatan);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============
        // EDIT (POST)
        // ============
        [HttpPost("Edit/{id:int}"), ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Edit(int id, Peralatan peralatan, List<IFormFile> files, bool? replacePhotos)
        {
            if (id != peralatan.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                SetApiBase(); await LoadDropdownData();
                return View(peralatan);
            }

            try
            {
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(peralatan.Kode ?? ""), "Kode");
                form.Add(new StringContent(peralatan.JenisId.ToString()), "JenisId");
                form.Add(new StringContent(peralatan.LokasiId.ToString()), "LokasiId");
                form.Add(new StringContent(peralatan.Spesifikasi ?? ""), "Spesifikasi");
                form.Add(new StringContent((replacePhotos ?? false).ToString().ToLowerInvariant()), "replacePhotos");

                if (peralatan.Exp_Date.HasValue)
                    form.Add(new StringContent(peralatan.Exp_Date.Value.ToString("yyyy-MM-dd")), "exp_date");

                if (!string.IsNullOrWhiteSpace(peralatan.Status))
                    form.Add(new StringContent(peralatan.Status), "status");
                if (!string.IsNullOrWhiteSpace(peralatan.Keterangan))
                    form.Add(new StringContent(peralatan.Keterangan), "keterangan");

                if (files != null)
                {
                    foreach (var f in files.Where(f => f?.Length > 0))
                    {
                        var sc = new StreamContent(f.OpenReadStream());
                        sc.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(f.ContentType ?? "application/octet-stream");
                        form.Add(sc, "files", f.FileName);
                    }
                }

                var resp = await _httpClient.PutAsync($"api/peralatan/admin/{id}", form);
                if (!resp.IsSuccessStatusCode)
                {
                    ViewBag.Error = await ReadApiErrorAsync(resp, "Gagal update peralatan");
                    SetApiBase(); await LoadDropdownData();
                    return View(peralatan);
                }

                TempData["Success"] = "Peralatan berhasil diupdate";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                SetApiBase(); await LoadDropdownData();
                return View(peralatan);
            }
        }

        // ============================
        // MARK AS EXPIRED (DangerZone)
        // ============================
        [HttpPost("MarkExpired"), ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> MarkExpired(int id, string reason)
        {
            try
            {
                var get = await _httpClient.GetAsync($"api/peralatan/admin/{id}");
                if (!get.IsSuccessStatusCode)
                {
                    TempData["Error"] = await ReadApiErrorAsync(get, "Peralatan tidak ditemukan");
                    return RedirectToAction(nameof(Index));
                }
                var json = await get.Content.ReadAsStringAsync();
                var alat = JsonConvert.DeserializeObject<Peralatan>(json) ?? new();

                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(alat.Kode ?? ""), "Kode");
                form.Add(new StringContent(alat.JenisId.ToString()), "JenisId");
                form.Add(new StringContent(alat.LokasiId.ToString()), "LokasiId");
                form.Add(new StringContent(alat.Spesifikasi ?? ""), "Spesifikasi");
                form.Add(new StringContent((alat.Exp_Date ?? DateTime.Today).ToString("yyyy-MM-dd")), "exp_date");
                form.Add(new StringContent("Exp"), "status");
                form.Add(new StringContent(reason ?? ""), "keterangan");

                var resp = await _httpClient.PutAsync($"api/peralatan/admin/{id}", form);
                if (!resp.IsSuccessStatusCode)
                {
                    TempData["Error"] = await ReadApiErrorAsync(resp, "Gagal menandai sebagai Exp");
                    return RedirectToAction(nameof(Edit), new { id });
                }

                TempData["Success"] = "Status peralatan diubah menjadi Exp dan timestamp diperbarui.";
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // =====================
        // FOTO: append & delete
        // =====================
        [HttpPost("AddPhotos/{id:int}"), ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> AddPhotos(int id, List<IFormFile> files)
        {
            try
            {
                using var form = new MultipartFormDataContent();
                if (files != null)
                {
                    foreach (var f in files.Where(f => f?.Length > 0))
                    {
                        var sc = new StreamContent(f.OpenReadStream());
                        sc.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(f.ContentType ?? "application/octet-stream");
                        form.Add(sc, "files", f.FileName);
                    }
                }

                var resp = await _httpClient.PostAsync($"api/peralatan/admin/{id}/photos", form);
                TempData[resp.IsSuccessStatusCode ? "Success" : "Error"] =
                    resp.IsSuccessStatusCode ? "Foto berhasil ditambahkan" :
                    await ReadApiErrorAsync(resp, "Gagal menambah foto");

                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        [HttpPost("DeletePhoto/{id:int}"), ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> DeletePhoto(int id, string path)
        {
            try
            {
                var payload = new { path };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var req = new HttpRequestMessage(HttpMethod.Delete, $"api/peralatan/admin/{id}/photos") { Content = content };
                var resp = await _httpClient.SendAsync(req);

                TempData[resp.IsSuccessStatusCode ? "Success" : "Error"] =
                    resp.IsSuccessStatusCode ? "Foto dihapus" :
                    await ReadApiErrorAsync(resp, "Gagal menghapus foto");

                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // samakan "Delete" dengan mark expired (opsional)
        [HttpPost("Delete/{id:int}"), ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public Task<IActionResult> Delete(int id, string? reason)
            => MarkExpired(id, reason ?? "Ditandai Exp via daftar peralatan");

        // =========
        // QR & VIEW
        // =========
        [HttpGet("QR/{id:int}")]
        [Authorize(Roles = "AdminWeb,Rescue")]
        public async Task<IActionResult> QR(int id)
        {
            try
            {
                SetApiBase();

                var resp = await _httpClient.GetAsync($"api/peralatan/admin/{id}/qr");
                if (!resp.IsSuccessStatusCode)
                {
                    TempData["Error"] = await ReadApiErrorAsync(resp, "Peralatan tidak ditemukan");
                    return RedirectToAction(nameof(Index));
                }

                var json = await resp.Content.ReadAsStringAsync();

                QRCodeResponse? qrData;
                try
                {
                    qrData = JsonConvert.DeserializeObject<QRCodeResponse>(json);
                    if (qrData?.TokenQR == Guid.Empty)
                    {
                        dynamic wrap = JsonConvert.DeserializeObject(json)!;
                        qrData = wrap?.data != null
                            ? JsonConvert.DeserializeObject<QRCodeResponse>(wrap.data.ToString())
                            : qrData;
                    }
                }
                catch
                {
                    dynamic wrap = JsonConvert.DeserializeObject(json)!;
                    qrData = wrap?.data != null
                        ? JsonConvert.DeserializeObject<QRCodeResponse>(wrap.data.ToString())
                        : null;
                }

                if (qrData == null || qrData.TokenQR == Guid.Empty)
                {
                    TempData["Error"] = "QR data tidak valid dari API";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrWhiteSpace(qrData.QrUrl))
                {
                    var apiBase = (ViewBag.ApiBase as string) ?? _httpClient.BaseAddress?.ToString()?.TrimEnd('/');
                    qrData.QrUrl = $"{apiBase}/api/peralatan/with-checklist?token={qrData.TokenQR}";
                }

                if (string.IsNullOrWhiteSpace(qrData.QrData))
                {
                    qrData.QrData = JsonConvert.SerializeObject(new { t = qrData.TokenQR, k = qrData.Kode, v = 1 });
                }

                return View(qrData);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("Details/{id:int}")]
        [Authorize(Roles = "AdminWeb,Rescue")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                SetApiBase();

                var peralatanRes = await _httpClient.GetAsync($"api/peralatan/admin/{id}");
                if (!peralatanRes.IsSuccessStatusCode)
                {
                    TempData["Error"] = await ReadApiErrorAsync(peralatanRes, "Peralatan tidak ditemukan");
                    return RedirectToAction(nameof(Index));
                }
                var peralatanJson = await peralatanRes.Content.ReadAsStringAsync();
                var peralatan = JsonConvert.DeserializeObject<Peralatan>(peralatanJson) ?? new Peralatan();

                var historyRes = await _httpClient.GetAsync($"api/perawatan/history/{id}");
                var riwayat = new List<Maintenance>();
                if (historyRes.IsSuccessStatusCode)
                {
                    var histJson = await historyRes.Content.ReadAsStringAsync();
                    dynamic wrapper = JsonConvert.DeserializeObject(histJson)!;
                    if (wrapper != null && wrapper.data != null)
                        riwayat = JsonConvert.DeserializeObject<List<Maintenance>>(wrapper.data.ToString()) ?? new List<Maintenance>();
                }

                var vm = new PeralatanDetailsViewModel { Peralatan = peralatan, Riwayat = riwayat };
                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // =================
        // Helpers (dropdown)
        // =================
        private async Task LoadDropdownData()
        {
            try
            {
                // ==== Lokasi (ikutkan DetailNamaLokasi) ====
                var lokasiResponse = await _httpClient.GetAsync("api/lokasi");
                if (lokasiResponse.IsSuccessStatusCode)
                {
                    var lokasiJson = await lokasiResponse.Content.ReadAsStringAsync();
                    var lokasiList = JsonConvert.DeserializeObject<List<Lokasi>>(lokasiJson) ?? new List<Lokasi>();
                    ViewBag.LokasiList = lokasiList
                        .Select(l => new DropdownItem
                        {
                            Id = l.Id,
                            Nama = l.Nama ?? string.Empty,
                            DetailNamaLokasi = l.DetailNamaLokasi ?? string.Empty // ✅ kunci
                        })
                        .OrderBy(x => x.Nama)
                        .ThenBy(x => x.DetailNamaLokasi)
                        .ToList();
                }
                else ViewBag.LokasiList = new List<DropdownItem>();

                // ==== Jenis (tetap) ====
                var jenisResponse = await _httpClient.GetAsync("api/jenis-peralatan");
                if (jenisResponse.IsSuccessStatusCode)
                {
                    var jenisJson = await jenisResponse.Content.ReadAsStringAsync();
                    var jenisList = JsonConvert.DeserializeObject<List<JenisPeralatan>>(jenisJson) ?? new List<JenisPeralatan>();
                    ViewBag.JenisList = jenisList.Select(j => new DropdownItem
                    {
                        Id = j.Id,
                        Nama = $"{j.Nama} ({j.IntervalPemeriksaanBulan} bulan)"
                    }).OrderBy(x => x.Nama).ToList();
                }
                else ViewBag.JenisList = new List<DropdownItem>();
            }
            catch
            {
                ViewBag.LokasiList = new List<DropdownItem>();
                ViewBag.JenisList = new List<DropdownItem>();
            }
        }

        private async Task LoadFilterDropdownDataForIndex(List<Peralatan> data)
        {
            try
            {
                var jenisResp = await _httpClient.GetAsync("api/jenis-peralatan");
                if (jenisResp.IsSuccessStatusCode)
                {
                    var jenisJson = await jenisResp.Content.ReadAsStringAsync();
                    var jenisList = JsonConvert.DeserializeObject<List<JenisPeralatan>>(jenisJson) ?? new List<JenisPeralatan>();
                    ViewBag.FilterJenis = jenisList.Select(j => new DropdownItem { Id = j.Id, Nama = j.Nama })
                                                   .OrderBy(x => x.Nama).ToList();
                }
                else
                {
                    ViewBag.FilterJenis = data.Where(x => x.JenisId != 0 && !string.IsNullOrWhiteSpace(x.JenisNama))
                                              .GroupBy(x => new { x.JenisId, x.JenisNama })
                                              .Select(g => new DropdownItem { Id = g.Key.JenisId, Nama = g.Key.JenisNama! })
                                              .OrderBy(x => x.Nama).ToList();
                }

                var lokasiResp = await _httpClient.GetAsync("api/lokasi");
                if (lokasiResp.IsSuccessStatusCode)
                {
                    var lokasiJson = await lokasiResp.Content.ReadAsStringAsync();
                    var lokasiList = JsonConvert.DeserializeObject<List<Lokasi>>(lokasiJson) ?? new List<Lokasi>();
                    ViewBag.FilterLokasi = lokasiList
                        .Select(l => new DropdownItem
                        {
                            Id = l.Id,
                            Nama = l.Nama ?? string.Empty,
                            DetailNamaLokasi = l.DetailNamaLokasi ?? string.Empty // ✅ dibawa juga utk tampilan filter (kalau mau dipakai)
                        })
                        .OrderBy(x => x.Nama)
                        .ThenBy(x => x.DetailNamaLokasi)
                        .ToList();
                }
                else
                {
                    // fallback dari data list (tidak punya detail lokasi di payload lama)
                    ViewBag.FilterLokasi = data.Where(x => x.LokasiId != 0 && !string.IsNullOrWhiteSpace(x.LokasiNama))
                                               .GroupBy(x => new { x.LokasiId, x.LokasiNama })
                                               .Select(g => new DropdownItem { Id = g.Key.LokasiId, Nama = g.Key.LokasiNama! })
                                               .OrderBy(x => x.Nama).ToList();
                }
            }
            catch
            {
                ViewBag.FilterJenis = new List<DropdownItem>();
                ViewBag.FilterLokasi = new List<DropdownItem>();
            }
        }

        private static async Task<string> ReadApiErrorAsync(HttpResponseMessage resp, string fallback)
        {
            try
            {
                var txt = await resp.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    dynamic o = JsonConvert.DeserializeObject(txt)!;
                    var msg = (string?)o?.message;
                    if (!string.IsNullOrWhiteSpace(msg)) return msg!;
                }
            }
            catch { }
            return $"{fallback} (HTTP {(int)resp.StatusCode})";
        }
    }
}
