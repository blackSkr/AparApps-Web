// Controllers/PeralatanController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AparAppsWebsite.Models;
using System.Text;

namespace AparWebAdmin.Controllers
{
    [Authorize] // 🔒 Wajib login
    public class PeralatanController : Controller
    {
        private readonly HttpClient _httpClient;

        public PeralatanController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        private void SetApiBase() => ViewBag.ApiBase = _httpClient.BaseAddress?.ToString()?.TrimEnd('/');

        // LIST
        [Authorize(Roles = "AdminWeb,Rescue")]
        public async Task<IActionResult> Index()
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
                var peralatan = JsonConvert.DeserializeObject<List<Peralatan>>(json) ?? new List<Peralatan>();
                await LoadFilterDropdownDataForIndex(peralatan);
                return View(peralatan);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                return View(new List<Peralatan>());
            }
        }

        // CREATE GET
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Create()
        {
            SetApiBase();
            await LoadDropdownData();
            return View();
        }

        // CREATE POST
        [HttpPost, ValidateAntiForgeryToken]
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

                var responseBody = await resp.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody)!;
                TempData["Success"] = "Peralatan berhasil ditambahkan";
                TempData["TokenQR"] = (string?)result?.tokenQR;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                SetApiBase(); await LoadDropdownData();
                return View(peralatan);
            }
        }

        // EDIT GET
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

        // EDIT POST
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Edit(int id, Peralatan peralatan, List<IFormFile> files, bool? replacePhotos)
        {
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

        [HttpPost, ValidateAntiForgeryToken]
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

        [HttpPost, ValidateAntiForgeryToken]
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

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "AdminWeb")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/peralatan/admin/{id}");
                TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                    response.IsSuccessStatusCode ? "Peralatan berhasil dihapus" :
                    await ReadApiErrorAsync(response, "Gagal menghapus peralatan");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // ====== QR VIEW ======
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

                // Support: langsung QRCodeResponse atau wrapper { data: {...} }
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

                // Fallback QrUrl jika backend belum menyuplai
                if (string.IsNullOrWhiteSpace(qrData.QrUrl))
                {
                    var apiBase = (ViewBag.ApiBase as string) ?? _httpClient.BaseAddress?.ToString()?.TrimEnd('/');
                    qrData.QrUrl = $"{apiBase}/api/peralatan/with-checklist?token={qrData.TokenQR}";
                }

                // Fallback QrData agar ringkas
                if (string.IsNullOrWhiteSpace(qrData.QrData))
                {
                    qrData.QrData = JsonConvert.SerializeObject(new
                    {
                        t = qrData.TokenQR,
                        k = qrData.Kode,
                        v = 1
                    });
                }

                return View(qrData);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

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

                // opsional riwayat
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

        // ===== Helpers =====
        private async Task LoadDropdownData()
        {
            try
            {
                var lokasiResponse = await _httpClient.GetAsync("api/lokasi");
                if (lokasiResponse.IsSuccessStatusCode)
                {
                    var lokasiJson = await lokasiResponse.Content.ReadAsStringAsync();
                    var lokasiList = JsonConvert.DeserializeObject<List<Lokasi>>(lokasiJson) ?? new List<Lokasi>();
                    ViewBag.LokasiList = lokasiList.Select(l => new DropdownItem { Id = l.Id, Nama = l.Nama }).ToList();
                }
                else ViewBag.LokasiList = new List<DropdownItem>();

                var jenisResponse = await _httpClient.GetAsync("api/jenis-peralatan");
                if (jenisResponse.IsSuccessStatusCode)
                {
                    var jenisJson = await jenisResponse.Content.ReadAsStringAsync();
                    var jenisList = JsonConvert.DeserializeObject<List<JenisPeralatan>>(jenisJson) ?? new List<JenisPeralatan>();
                    ViewBag.JenisList = jenisList.Select(j => new DropdownItem
                    {
                        Id = j.Id,
                        Nama = $"{j.Nama} ({j.IntervalPemeriksaanBulan} bulan)"
                    }).ToList();
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
                    ViewBag.FilterLokasi = lokasiList.Select(l => new DropdownItem { Id = l.Id, Nama = l.Nama })
                                                     .OrderBy(x => x.Nama).ToList();
                }
                else
                {
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
