namespace AparAppsWebsite.Models
{
    public class Petugas
    {
        public int Id { get; set; }
        public string? EmployeeNama { get; set; }
        public string BadgeNumber { get; set; } = "";
        public int? RolePetugasId { get; set; }
        public string? RoleNama { get; set; }
        public int? LokasiId { get; set; }
        public string? LokasiNama { get; set; }
    }
}
