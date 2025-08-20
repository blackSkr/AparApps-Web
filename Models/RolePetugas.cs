using System.ComponentModel.DataAnnotations;

namespace AparAppsWebsite.Models
{
    public class RolePetugas
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string NamaRole { get; set; } = string.Empty;

        public int? IntervalPetugasId { get; set; }

        [StringLength(500)]
        public string? Deskripsi { get; set; }

        public bool IsActive { get; set; } = true;

        // ----- display / computed dari BE -----
        public string? IntervalNama { get; set; }
        public int? Bulan { get; set; }
        public int? JumlahPetugas { get; set; }
    }

    // Kalau belum ada generic ini di project kamu, aktifkan:
    // public class PagedResult<T>
    // {
    //     public int page { get; set; }
    //     public int pageSize { get; set; }
    //     public int total { get; set; }
    //     public List<T> items { get; set; } = new();
    // }

    // public class DropdownItem
    // {
    //     public int Id { get; set; }
    //     public string Nama { get; set; } = "";
    //     public string? Extra { get; set; }
    // }
}
