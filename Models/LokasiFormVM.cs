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

        // ⛔ Tanpa [Range] supaya tidak memicu validasi client-side
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

        /// <summary>
        /// Normalisasi koordinat:
        /// - Ganti koma -> titik (di controller & view juga disanitasi)
        /// - Auto-swap jika kebalik (lat > 90 dan lon <= 90)
        /// - Pembulatan 6 desimal
        /// - Clamp ke rentang valid jika masih out-of-range tipis
        /// </summary>
        public (double? lat6, double? lon6) NormalizeLatLon()
        {
            double? la = lat, lo = @long;

            // Auto-swap jika kebalik
            if (la.HasValue && lo.HasValue && Math.Abs(la.Value) > 90 && Math.Abs(lo.Value) <= 90)
            { var t = la; la = lo; lo = t; }

            static double? R6(double? x)
            {
                if (x is null) return null;
                var r = Math.Round(x.Value, 6, MidpointRounding.AwayFromZero);
                return r == 0 ? 0 : r;
            }

            // Clamp lembut supaya tetap valid
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
