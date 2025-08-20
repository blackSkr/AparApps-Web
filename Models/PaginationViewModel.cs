// Models/PaginationViewModel.cs
namespace AparAppsWebsite.Models
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public string ItemName { get; set; } = "items";
        public string BaseUrl { get; set; } = "";

        public int StartItem => (CurrentPage - 1) * PageSize + 1;
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public int StartPage => Math.Max(1, CurrentPage - 2);
        public int EndPage => Math.Min(TotalPages, CurrentPage + 2);

        public string PreviousPageUrl => HasPreviousPage ? GetPageUrl(CurrentPage - 1) : "#";
        public string NextPageUrl => HasNextPage ? GetPageUrl(CurrentPage + 1) : "#";

        public string GetPageUrl(int page)
        {
            return $"{BaseUrl}?page={page}&pageSize={PageSize}";
        }
    }
}