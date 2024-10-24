using System.Security.Claims;

namespace TaskManager.Helpers
{
    public static class ClaimsPrincipalExtensions {
        public static int? GetUserId(this ClaimsPrincipal user) {
            // Get the logged-in user's ID from Claims
            var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if ((!string.IsNullOrEmpty(userIdString)) && (int.TryParse(userIdString, out int userId))) {
                return userId;
            }

            return null;
        }
    }
}