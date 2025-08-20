namespace AparAppsWebsite.Models
{
    public class LokasiFormRequest
    {
        public string nama { get; set; } = "";
        public double? lat { get; set; }
        public double? @long { get; set; }
        public int? picPetugasId { get; set; }
    }
}
