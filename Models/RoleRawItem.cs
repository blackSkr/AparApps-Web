namespace AparAppsWebsite.Models
{
    public class RoleRawItem
    {
        public int Id { get; set; }
        public string NamaRole { get; set; } = "";
        public int? IntervalPetugasId { get; set; }
        public string? NamaInterval { get; set; }
        public int? Bulan { get; set; }
    }
}
