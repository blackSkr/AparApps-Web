using System.Security.Claims;

namespace AparAppsWebsite.Utils
{
    public static class UserExtensions
    {
        /// <summary>
        /// Ambil BadgeNumber dari klaim login.
        /// </summary>
        public static string? Badge(this ClaimsPrincipal? user)
            => user?.FindFirst("BadgeNumber")?.Value;

        /// <summary>
        /// True jika user punya role AdminWeb.
        /// </summary>
        public static bool IsAdminWeb(this ClaimsPrincipal? user)
            => user?.IsInRole("AdminWeb") == true;

        /// <summary>
        /// True jika user punya role Rescue.
        /// </summary>
        public static bool IsRescue(this ClaimsPrincipal? user)
            => user?.IsInRole("Rescue") == true;
    }
}
