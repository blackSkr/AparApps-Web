namespace AparAppsWebsite.Models
{
    // public class PetugasOption
    // {
    //     public int Id { get; set; }
    //     public string BadgeNumber { get; set; } = "";
    //     public string EmployeeNama { get; set; } = "";
    //     public string? RoleNama { get; set; }
    // }

    public class LokasiFormMeta
    {
        // Sesuai /api/lokasi/form-meta pada BE Node
        public List<PetugasOption> petugasTanpaLokasi { get; set; } = new();
        // (opsional) properti lain dari BE boleh diabaikan
    }
}
