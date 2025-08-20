using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AparAppsWebsite.Models
{
    public class PetugasEditVm
    {
        public int Id { get; set; }

        // hanya untuk display (readonly di view)
        public string? EmployeeNama { get; set; }
        public string? BadgeNumber { get; set; }

        [Required(ErrorMessage = "Role wajib dipilih.")]
        public int? RolePetugasId { get; set; }

        public int? LokasiId { get; set; }

        // dropdown data
        public List<RoleItem> Roles { get; set; } = new();
        public List<LokasiItem> Lokasi { get; set; } = new();
    }
}
