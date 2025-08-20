// Models/ViewModels/DashboardViewModel.cs
public class DashboardViewModel
{
    public int? TotalPeralatan { get; set; }
    public int? TotalChecklist { get; set; }
    public int? TotalLokasi { get; set; }
    public int? TotalPetugas { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public string? ErrorMessage { get; set; }

    // Panel debug
    public string? DebugInfo { get; set; }
}
