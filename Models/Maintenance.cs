using System.ComponentModel.DataAnnotations;

namespace AparAppsWebsite.Models
{
    public class Maintenance
    {
        public int Id { get; set; }

        [Required]
        public int PeralatanId { get; set; }

        [Required, StringLength(100)]
        public string BadgeNumber { get; set; } = string.Empty;

        [Required]
        public DateTime TanggalPemeriksaan { get; set; }

        public int? IntervalPetugasId { get; set; }

        [StringLength(100)]
        public string Kondisi { get; set; } = string.Empty;

        [StringLength(500)]
        public string CatatanMasalah { get; set; } = string.Empty;

        [StringLength(500)]
        public string Rekomendasi { get; set; } = string.Empty;

        [StringLength(500)]
        public string TindakLanjut { get; set; } = string.Empty;

        public float? Tekanan { get; set; }
        public int? JumlahMasalah { get; set; }

        // New from BE
        public string AparKode { get; set; } = string.Empty;
        public string LokasiNama { get; set; } = string.Empty;
        public string JenisNama { get; set; } = string.Empty;
        public string PetugasBadge { get; set; } = string.Empty;
        public string PetugasRole { get; set; } = string.Empty;
        public string NamaInterval { get; set; } = string.Empty;
        public int? IntervalBulan { get; set; }
        public string Spesifikasi { get; set; } = string.Empty;

        // NEW: koordinat hasil inspeksi
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public List<ChecklistJawaban> Checklist { get; set; } = new();
        public List<FotoPemeriksaan> Photos { get; set; } = new();

        // Calculated
        public int IntervalHari => (IntervalBulan ?? 1) * 30;
        public DateTime NextDueDate => TanggalPemeriksaan.AddDays(IntervalHari);
        public bool IsOverdue => NextDueDate < DateTime.Now;
        public bool IsDueSoon => NextDueDate <= DateTime.Now.AddDays(7) && !IsOverdue;

        public string StatusBadge => IsOverdue ? "danger" : IsDueSoon ? "warning" : "success";
        public string StatusText => IsOverdue ? "Overdue" : IsDueSoon ? "Due Soon" : "OK";

        public string KondisiBadge => (Kondisi ?? "").Trim() switch
        {
            "Baik" => "success",
            "Rusak" => "danger",
            "Perlu Perbaikan" => "warning",
            _ => "secondary"
        };

        // NEW: flag “bermasalah”
        public bool HasProblem => !string.Equals(Kondisi ?? "", "Baik", StringComparison.OrdinalIgnoreCase)
                                  || (JumlahMasalah ?? 0) > 0;

        // Helper display
        public string? GoogleMapsUrl => (Latitude.HasValue && Longitude.HasValue)
            ? $"https://www.google.com/maps?q={Latitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Longitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            : null;
    }
}
