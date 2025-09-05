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

        public string? DetailNamaLokasi { get; set; }  // ✅ baru

        public string? Spesifikasi { get; set; }
        public string? TokenQR { get; set; }

        public string? Status { get; set; }
        public DateTime? Tanggal_Update_Alat { get; set; }
        public string? Keterangan { get; set; }
        public DateTime? Exp_Date { get; set; }

        public string? FotoPath { get; set; }
        public List<string>? FotoUrls { get; set; }
    }
}
