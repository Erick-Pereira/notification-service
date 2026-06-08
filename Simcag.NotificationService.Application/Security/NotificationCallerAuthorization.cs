using System.Security.Claims;
using Simcag.Shared.Security;

namespace Simcag.NotificationService.Application.Security;

/// <summary>
/// Garante que utilizadores não-admin só acedem às próprias preferências e entregas.
/// </summary>
public static class NotificationCallerAuthorization
{
    public static Guid? ResolveCallerUserId(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
    }

    public static bool IsAdmin(ClaimsPrincipal? user) =>
        user?.IsInRole(SimcagRoles.Admin) == true
        || string.Equals(user?.FindFirst(ClaimTypes.Role)?.Value, SimcagRoles.Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(user?.FindFirst("role")?.Value, SimcagRoles.Admin, StringComparison.OrdinalIgnoreCase);

    public static bool CanAccessUserData(ClaimsPrincipal? user, Guid targetUserId)
    {
        if (IsAdmin(user))
            return true;

        var callerId = ResolveCallerUserId(user);
        return callerId.HasValue && callerId.Value == targetUserId;
    }
}
