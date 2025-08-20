using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AparAppsWebsite.Models
{
    public class PetugasCreateVm
    {
        // Dipertahankan agar struktur lama tidak rusak (TIDAK dipakai di view)
        [StringLength(50)]
        public string? BadgeNumber { get; set; } = null;

        // Baru: sumber kebenaran dari dropdown Employee
        [Required(ErrorMessage = "Employee wajib dipilih.")]
        public int? EmployeeId { get; set; }

        [Required]
        public int? RolePetugasId { get; set; }

        public List<RoleItem> Roles { get; set; } = new();
    }
}
