using System.Security.Claims;
using FluentAssertions;
using Simcag.NotificationService.Application.Security;
using Simcag.Shared.Security;

namespace Simcag.NotificationService.Tests;

public sealed class NotificationCallerAuthorizationTests
{
    private static ClaimsPrincipal Principal(string userId, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public void CanAccessUserData_AllowsOwnPreferences_ForSindico()
    {
        var userId = Guid.NewGuid();
        var user = Principal(userId.ToString(), SimcagRoles.Sindico);

        NotificationCallerAuthorization.CanAccessUserData(user, userId).Should().BeTrue();
    }

    [Fact]
    public void CanAccessUserData_DeniesOtherUser_ForSindico()
    {
        var user = Principal(Guid.NewGuid().ToString(), SimcagRoles.Sindico);

        NotificationCallerAuthorization.CanAccessUserData(user, Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void CanAccessUserData_AllowsAnyUser_ForAdmin()
    {
        var admin = Principal(Guid.NewGuid().ToString(), SimcagRoles.Admin);

        NotificationCallerAuthorization.CanAccessUserData(admin, Guid.NewGuid()).Should().BeTrue();
    }
}
