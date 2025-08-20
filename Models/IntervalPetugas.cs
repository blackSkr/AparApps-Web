using System.ComponentModel.DataAnnotations;

namespace AparAppsWebsite.Models
{
    public class IntervalPetugas
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama interval harus diisi")]
        [StringLength(100, ErrorMessage = "Nama interval maksimal 100 karakter")]
        [Display(Name = "Nama Interval")]
        public string NamaInterval { get; set; } = string.Empty;

        [Range(0, 12, ErrorMessage = "Bulan harus antara 0-12")]
        [Display(Name = "Interval (Bulan)")]
        public int? Bulan { get; set; }

        [Display(Name = "Deskripsi")]
        public string Deskripsi => Bulan == 0 ? "Interval Khusus" : $"Setiap {Bulan} bulan";
    }
}