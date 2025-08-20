// Models/FotoPemeriksaan.cs
namespace AparAppsWebsite.Models
{
    public class FotoPemeriksaan
    {
        public int Id { get; set; }
        public int PemeriksaanId { get; set; }
        public string FotoPath { get; set; } = "";
        public DateTime UploadedAt { get; set; }

        // Display Properties
        public string FileName => Path.GetFileName(FotoPath);
        public string FileExtension => Path.GetExtension(FotoPath);
        public bool IsImage => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }
            .Contains(FileExtension.ToLower());
    }
}



