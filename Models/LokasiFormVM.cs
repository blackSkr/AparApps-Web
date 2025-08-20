namespace AparAppsWebsite.Models
{
    public class LokasiFormVM
    {
        public int Id { get; set; }
        public string? Nama { get; set; }

        public double? lat { get; set; }
        public double? @long { get; set; }

        // ✅ Alias aman untuk Razor
        public double? Longitude
        {
            get => @long;
            set => @long = value;
        }

        // PIC "utama" (opsional) — tetap dipakai untuk payload create/update agar kompatibel
        public int? PICPetugasId { get; set; }

        // ✅ List builder untuk banyak PIC saat Create
        public List<int> PICMultiIds { get; set; } = new();

        // Dropdown options
        public List<PetugasOption> PICOptions { get; set; } = new();

        // ✅ Daftar petugas yang sudah terhubung ke lokasi (untuk Edit/Details)
        public List<PetugasListItemVM> CurrentPetugas { get; set; } = new();
    }
}
