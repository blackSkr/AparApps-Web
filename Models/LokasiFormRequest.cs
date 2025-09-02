namespace AparAppsWebsite.Models
{
    // DTO untuk payload ke BE Node
    public class LokasiFormRequest
    {
        public string? nama { get; set; }

        // ✅ kirim sebagai camelCase yang baru: detailNamaLokasi
        // (BE akan terima ini & simpan ke kolom [detail_nama_lokasi])
        public string? detailNamaLokasi { get; set; }

        public double? lat { get; set; }
        public double? @long { get; set; }
        public int? picPetugasId { get; set; }
    }
}
