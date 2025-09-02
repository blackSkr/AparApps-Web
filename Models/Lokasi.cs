namespace AparAppsWebsite.Models
{
    public class Lokasi
    {
        public int Id { get; set; }

        // Nama utama lokasi
        public string Nama { get; set; } = "";

        // ✅ Baru: detail_nama_lokasi (DB) → properti C#:
        public string? DetailNamaLokasi { get; set; }

        // BE Node pakai DECIMAL(10,6); di C# gunakan decimal? supaya presisi
        public decimal? lat { get; set; }
        public decimal? @long { get; set; }

        public int? PICPetugasId { get; set; }
        public string? PIC_BadgeNumber { get; set; }
        public string? PIC_Jabatan { get; set; }
        public string? PIC_Nama { get; set; }
    }
}
