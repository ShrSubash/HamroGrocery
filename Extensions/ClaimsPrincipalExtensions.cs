using System.Security.Claims;

namespace HamroGrocery.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserIdInt(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value
                          ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (int.TryParse(idClaim, out int userId))
                return userId;

            throw new InvalidOperationException("User ID claim is missing or invalid.");
        }

        public static string? GetUserId(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}