// Models/PeralatanDetailsViewModel.cs
using System.Collections.Generic;

namespace AparAppsWebsite.Models
{
    public class PeralatanDetailsViewModel
    {
        public Peralatan? Peralatan { get; set; }
        public List<Maintenance> Riwayat { get; set; } = new();
    }
}
