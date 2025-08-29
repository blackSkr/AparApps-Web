using System.Security.Claims;

namespace AparAppsWebsite.Utils
{
    public static class UserExtensions
    {
        public static string? Badge(this ClaimsPrincipal user)
            => user.FindFirst("BadgeNumber")?.Value;

        public static bool IsAdminWeb(this ClaimsPrincipal user)
            => user.IsInRole("AdminWeb");
    }
}
