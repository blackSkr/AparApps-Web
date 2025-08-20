namespace AparAppsWebsite.Models
{
    public class LokasiListVM
    {
        public int page { get; set; }
        public int pageSize { get; set; }
        public int total { get; set; }
        public List<Lokasi> items { get; set; } = new();
    }
}
