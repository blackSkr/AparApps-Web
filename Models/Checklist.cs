// Models/Checklist.cs
using System.ComponentModel.DataAnnotations;

namespace AparAppsWebsite.Models
{
    public class Checklist
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Pertanyaan wajib diisi")]
        [Display(Name = "Pertanyaan Checklist")]
        [StringLength(500, ErrorMessage = "Pertanyaan maksimal 500 karakter")]
        public string Pertanyaan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jenis peralatan wajib dipilih")]
        [Display(Name = "Jenis Peralatan")]
        public int JenisId { get; set; }

        // Navigation property untuk display
        public string? JenisNama { get; set; }
    }

    // DTO untuk bulk create
    public class BulkChecklistCreate
    {
        [Required(ErrorMessage = "Jenis peralatan wajib dipilih")]
        public int JenisId { get; set; }

        [Required(ErrorMessage = "Minimal 1 pertanyaan harus diisi")]
        public List<string> Pertanyaan { get; set; } = new List<string>();

        // Navigation property
        public string? JenisNama { get; set; }
    }
}