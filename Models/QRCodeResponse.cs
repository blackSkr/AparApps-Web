// Models/QRCodeResponse.cs - Enhanced
namespace AparAppsWebsite.Models
{
    public class QRCodeResponse
    {
        public Guid TokenQR { get; set; }
        public string Kode { get; set; } = string.Empty;
        public string QrData { get; set; } = string.Empty;
        public string QrUrl { get; set; } = string.Empty;

        // Additional professional features
        public Dictionary<string, string>? QrUrls { get; set; }
        public QRMetadata? Metadata { get; set; }
    }

    public class QRMetadata
    {
        public string? Lokasi { get; set; }
        public string? Jenis { get; set; }
        public DateTime Generated { get; set; }
    }
}