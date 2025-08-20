// Models/JenisPeralatan.cs
using System.ComponentModel.DataAnnotations;

namespace AparAppsWebsite.Models
{
    public class JenisPeralatan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama jenis peralatan wajib diisi")]
        [Display(Name = "Nama Jenis Peralatan")]
        public string Nama { get; set; } = string.Empty;

        [Required(ErrorMessage = "Interval pemeriksaan wajib diisi")]
        [Range(1, 60, ErrorMessage = "Interval pemeriksaan harus antara 1-60 bulan")]
        [Display(Name = "Interval Pemeriksaan (Bulan)")]
        public int IntervalPemeriksaanBulan { get; set; }
    }
}