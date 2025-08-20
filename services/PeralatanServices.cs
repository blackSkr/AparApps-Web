using AparAppsWebsite.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace AparWebAdmin.Services
{
    public class PeralatanService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "peralatan_admin_all";

        public PeralatanService(IHttpClientFactory httpFactory, IMemoryCache cache)
        {
            _httpFactory = httpFactory;
            _cache = cache;
        }

        // Ambil semua dari cache (refresh tiap 5 menit / sliding 2 menit)
        private async Task<List<Peralatan>> GetAllAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<Peralatan>? cached) && cached != null)
                return cached;

            var client = _httpFactory.CreateClient("ApiClient");
            var resp = await client.GetAsync("api/peralatan/admin");
            if (!resp.IsSuccessStatusCode) return new List<Peralatan>();

            var json = await resp.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<Peralatan>>(json) ?? new List<Peralatan>();

            _cache.Set(CacheKey, list, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Size = list.Count // opsional jika SizeLimit dipakai
            });

            return list;
        }

        public async Task<PagedResult<Peralatan>> GetPagedAsync(
            int page = 1, int pageSize = 25,
            string? query = null, string? sortBy = null, bool desc = false)
        {
            var all = await GetAllAsync();

            // Search ringan (kode/jenis/lokasi/spesifikasi)
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLowerInvariant();
                all = all.Where(x =>
                       (x.Kode ?? "").ToLower().Contains(q)
                    || (x.JenisNama ?? "").ToLower().Contains(q)
                    || (x.LokasiNama ?? "").ToLower().Contains(q)
                    || (x.Spesifikasi ?? "").ToLower().Contains(q)
                ).ToList();
            }

            // Sorting ringan
            IOrderedEnumerable<Peralatan>? ordered = null;
            switch ((sortBy ?? "").ToLowerInvariant())
            {
                case "kode":
                    ordered = desc ? all.OrderByDescending(x => x.Kode)
                                   : all.OrderBy(x => x.Kode);
                    break;
                case "jenis":
                    ordered = desc ? all.OrderByDescending(x => x.JenisNama)
                                   : all.OrderBy(x => x.JenisNama);
                    break;
                case "lokasi":
                    ordered = desc ? all.OrderByDescending(x => x.LokasiNama)
                                   : all.OrderBy(x => x.LokasiNama);
                    break;
                default:
                    ordered = desc ? all.OrderByDescending(x => x.Id)
                                   : all.OrderBy(x => x.Id);
                    break;
            }

            var total = ordered.Count();
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 200); // paginasi aman

            var items = ordered.Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToList();

            return new PagedResult<Peralatan>
            {
                Items = items,
                TotalItems = total,
                Page = page,
                PageSize = pageSize,
                Query = query,
                SortBy = sortBy,
                SortDesc = desc
            };
        }

        // Untuk invalidasi manual (misal setelah Create/Update/Delete)
        public void InvalidateCache() => _cache.Remove(CacheKey);
    }
}
