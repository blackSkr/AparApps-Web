namespace AparAppsWebsite.Models
{
    public class LoginViewModel
    {
        public string? BadgeNumber { get; set; }
        public bool RememberMe { get; set; } = true;
        public string? ReturnUrl { get; set; }
    }
}
