// models/ChecklistJawaban.cs
using System.ComponentModel.DataAnnotations;

namespace AparAppsWebsite.Models
{
    public class ChecklistJawaban
    {
        public int Id { get; set; }

        [Required]
        public int ChecklistId { get; set; }

        [Required]
        public Guid PeralatanTokenQR { get; set; }

        [Required]
        public int PetugasId { get; set; }

        [Required(ErrorMessage = "Jawaban harus dipilih")]
        [Display(Name = "Jawaban")]
        public bool Jawaban { get; set; }

        [StringLength(500, ErrorMessage = "Alasan maksimal 500 karakter")]
        [Display(Name = "Alasan")]
        public string? Alasan { get; set; }

        [StringLength(500, ErrorMessage = "Keterangan maksimal 500 karakter")]
        [Display(Name = "Keterangan")]
        public string? Keterangan { get; set; }

        [Display(Name = "Tanggal Pemeriksaan")]
        public DateTime TanggalPemeriksaan { get; set; } = DateTime.Now;

        // Display only
        [Display(Name = "Pertanyaan")]
        public string? PertanyaanChecklist { get; set; }

        [Display(Name = "Kode Peralatan")]
        public string? KodePeralatan { get; set; }

        [Display(Name = "Nama Petugas")]
        public string? NamaPetugas { get; set; }

        // Calculated status
        public bool Dicentang => Jawaban;
        public string Status => Dicentang ? "Baik" : "Tidak Baik";
        public string StatusBadge => Dicentang ? "success" : "danger";
        public string StatusIcon => Dicentang ? "fas fa-check" : "fas fa-times";
    }
}
