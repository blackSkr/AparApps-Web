namespace AparAppsWebsite.Models
{
    public class PetugasListResponse
    {
        public int page { get; set; }
        public int pageSize { get; set; }
        public int total { get; set; }
        public List<Petugas> items { get; set; } = new();
    }
}
