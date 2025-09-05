namespace AparAppsWebsite.Models
{
    public class DropdownItem
    {
        public int Id { get; set; }
        public string Nama { get; set; } = string.Empty;
        public string DetailNamaLokasi { get; set; } = string.Empty;
        public string? Extra { get; set; } // biarkan untuk extensibility
    }
}
