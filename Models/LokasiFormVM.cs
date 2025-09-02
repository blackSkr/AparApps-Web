using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AparAppsWebsite.Models
{
    public class LokasiFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama lokasi wajib diisi.")]
        public string? Nama { get; set; }

        // ✅ Field baru (opsional)
        [Display(Name = "Detail Nama Lokasi (opsional)")]
        [MaxLength(500)]
        public string? DetailNamaLokasi { get; set; }

        [Display(Name = "Latitude")]
        public double? lat { get; set; }

        [Display(Name = "Longitude")]
        public double? @long { get; set; }

        // Alias aman untuk Razor
        public double? Longitude
        {
            get => @long;
            set => @long = value;
        }

        public int? PICPetugasId { get; set; }
        public List<int> PICMultiIds { get; set; } = new();
        public List<PetugasOption> PICOptions { get; set; } = new();
        public List<PetugasListItemVM> CurrentPetugas { get; set; } = new();

        public (double? lat6, double? lon6) NormalizeLatLon()
        {
            double? la = lat, lo = @long;

            if (la.HasValue && lo.HasValue && Math.Abs(la.Value) > 90 && Math.Abs(lo.Value) <= 90)
            { var t = la; la = lo; lo = t; }

            static double? R6(double? x)
            {
                if (x is null) return null;
                var r = Math.Round(x.Value, 6, MidpointRounding.AwayFromZero);
                return r == 0 ? 0 : r;
            }

            static double? Clamp(double? x, double min, double max)
            {
                if (!x.HasValue) return null;
                return Math.Max(min, Math.Min(max, x.Value));
            }

            var latClamped = Clamp(la, -90, 90);
            var lonClamped = Clamp(lo, -180, 180);

            return (R6(latClamped), R6(lonClamped));
        }
    }
}
