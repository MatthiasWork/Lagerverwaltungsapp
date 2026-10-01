using System.Security.Claims;

namespace Lagerverwaltungsapp_MatthiasUtrata.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Methode, die die ID der angemeldeten Person aus dem Sid-Claim liest, der beim Login gesetzt wird.
        /// </summary>
        /// <param name="user">Der angemeldete Benutzer</param>
        /// <returns>Die ID der Person oder null, wenn niemand angemeldet ist</returns>
        public static int? GetPersonID(this ClaimsPrincipal user)
        {
            if (int.TryParse(user.FindFirstValue(ClaimTypes.Sid), out var id))
            {
                return id;
            }
            return null;
        }
    }
}
