namespace AparAppsWebsite.Models
{
    public class Peralatan
    {
        public int Id { get; set; }
        public string? Kode { get; set; }

        public int JenisId { get; set; }
        public string? JenisNama { get; set; }

        public int LokasiId { get; set; }
        public string? LokasiNama { get; set; }

        public string? Spesifikasi { get; set; }
        public string? TokenQR { get; set; }

        // Tambahan agar Views tidak error
        public string? FotoPath { get; set; }                 // format "a;b;c" dari API Node
        public List<string>? FotoUrls { get; set; }           // quality-of-life dari /admin endpoint
    }
}
