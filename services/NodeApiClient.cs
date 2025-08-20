// Models/NodeApiClient.cs
using System.Net.Http.Json;
using System.Text.Json;

namespace AparAppsWebsite.Models
{
    /// <summary>
    /// Client sederhana untuk call BE Node (lokasi + relasi petugas).
    /// Letak file boleh di Models/ tapi namespace harus AparAppsWebsite.Models
    /// supaya DTO (Lokasi, Petugas, dsb) satu namespace dan langsung dikenal.
    /// </summary>
    public class NodeApiClient
    {
        private readonly HttpClient _http;
        public NodeApiClient(HttpClient http) => _http = http;

        // =======================
        // ======= Lokasi ========
        // =======================

        public async Task<List<Lokasi>> GetLokasiAsync()
            => await _http.GetFromJsonAsync<List<Lokasi>>("api/lokasi", JsonOptions()) ?? new();

        public async Task<(bool ok, string? err, Lokasi? lokasi)> CreateLokasiAsync(LokasiFormRequest req)
        {
            var resp = await _http.PostAsJsonAsync("api/lokasi", req);
            if (!resp.IsSuccessStatusCode) return (false, await ReadMessage(resp), null);

            var txt = await resp.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Lokasi>(txt, JsonOptions());
            return (true, null, data);
        }

        public async Task<(bool ok, string? err, Lokasi? lokasi)> UpdateLokasiAsync(int id, LokasiFormRequest req)
        {
            var resp = await _http.PutAsJsonAsync($"api/lokasi/{id}", req);
            if (!resp.IsSuccessStatusCode) return (false, await ReadMessage(resp), null);

            var txt = await resp.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Lokasi>(txt, JsonOptions());
            return (true, null, data);
        }

        public async Task<(bool ok, string? err)> DeleteLokasiAsync(int id)
        {
            var resp = await _http.DeleteAsync($"api/lokasi/{id}");
            if (!resp.IsSuccessStatusCode) return (false, await ReadMessage(resp));
            return (true, null);
        }

        /// <summary>
        /// Ambil lokasi + daftar petugas pada lokasi itu.
        /// </summary>
        public async Task<(Lokasi lokasi, List<Petugas> items)?> GetLokasiWithPetugasAsync(int id)
        {
            var lok = await _http.GetFromJsonAsync<Lokasi>($"api/lokasi/{id}", JsonOptions());
            if (lok == null) return null;

            var wrap = await _http.GetFromJsonAsync<LokasiPetugasWrap>($"api/lokasi/{id}/petugas", JsonOptions());
            return (lok, wrap?.items ?? new());
        }

        // =======================
        // ====== Form Meta ======
        // =======================

        public async Task<LokasiFormMeta> GetLokasiFormMetaAsync()
            => await _http.GetFromJsonAsync<LokasiFormMeta>("api/lokasi/form-meta", JsonOptions()) ?? new();

        // =======================
        // === Relasi Petugas ====
        // =======================

        public async Task<(bool ok, string? err)> AddPetugasToLokasiAsync(int lokasiId, int petugasId, bool asPIC)
        {
            var payload = new { petugasId, isPIC = asPIC };
            var resp = await _http.PostAsJsonAsync($"api/lokasi/{lokasiId}/petugas", payload);
            if (!resp.IsSuccessStatusCode) return (false, await ReadMessage(resp));
            return (true, null);
        }

        public async Task<(bool ok, string? err)> UnlinkPetugasFromLokasiAsync(int lokasiId, int petugasId)
        {
            var resp = await _http.DeleteAsync($"api/lokasi/{lokasiId}/petugas/{petugasId}");
            if (!resp.IsSuccessStatusCode) return (false, await ReadMessage(resp));
            return (true, null);
        }

        // =======================
        // ======= Helpers =======
        // =======================

        private static async Task<string?> ReadMessage(HttpResponseMessage resp)
{
    try
    {
        var txt = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(txt);
        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
            doc.RootElement.TryGetProperty("message", out var m) &&
            m.ValueKind == JsonValueKind.String)
        {
            return m.GetString();
        }
        return txt;
    }
    catch
    {
        return $"HTTP {(int)resp.StatusCode}";
    }
}


        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // bentuk response /api/lokasi/:id/petugas pada BE Node
        private class LokasiPetugasWrap
        {
            public Lokasi lokasi { get; set; } = new();
            public List<Petugas> items { get; set; } = new();
        }
    }
}
