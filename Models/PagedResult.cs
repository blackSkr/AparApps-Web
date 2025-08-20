// Models/PagedResult.cs
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AparAppsWebsite.Models
{
    public class PagedResult<T>
    {
        // BE kirim "items" (lowercase)
        [JsonProperty("items")]
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        // BE kirim "page"
        [JsonProperty("page")]
        public int Page { get; set; } = 1;

        // BE kirim "pageSize"
        [JsonProperty("pageSize")]
        public int PageSize { get; set; } = 50;

        // BE kirim "total" → kita simpan ke TotalItems
        [JsonProperty("total")]
        public int TotalItems { get; set; } = 0;

        [JsonIgnore] public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling((double)TotalItems / PageSize);
        [JsonIgnore] public bool HasPrevious => Page > 1;
        [JsonIgnore] public bool HasNext => Page < TotalPages;

        // opsional (tidak dari BE)
        [JsonIgnore] public string? CurrentQueryString { get; set; }
        [JsonIgnore] public string? Query { get; set; }
        [JsonIgnore] public string? SortBy { get; set; }
        [JsonIgnore] public bool SortDesc { get; set; }
    }
}
