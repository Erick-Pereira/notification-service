using Microsoft.Extensions.Logging.Abstractions;
using Simcag.NotificationService.Application.Abstractions;
using Simcag.NotificationService.Application.Configuration;
using Simcag.NotificationService.Application.Services;
using Xunit;

namespace Simcag.NotificationService.Tests;

public sealed class AlertNotificationRecipientResolverTests
{
    private static readonly Guid DefaultUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EventUser = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Sindico = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Conselho = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public async Task ResolveRecipientIdsAsync_merges_tenant_governance_and_uploader()
    {
        var identity = new FakeIdentityClient(TenantId, [Sindico, Conselho]);
        var resolver = CreateResolver(identity, new NotificationRecipientOptions { DefaultNotifyUserId = DefaultUser });

        var ids = await resolver.ResolveRecipientIdsAsync(EventUser, TenantId, null, CancellationToken.None);

        Assert.Equal(3, ids.Count);
        Assert.Contains(Sindico, ids);
        Assert.Contains(Conselho, ids);
        Assert.Contains(EventUser, ids);
    }

    [Fact]
    public async Task ResolveRecipientIdsAsync_uses_default_when_tenant_empty_and_no_uploader()
    {
        var identity = new FakeIdentityClient(TenantId, []);
        var resolver = CreateResolver(identity, new NotificationRecipientOptions { DefaultNotifyUserId = DefaultUser });

        var ids = await resolver.ResolveRecipientIdsAsync(null, null, null, CancellationToken.None);

        Assert.Single(ids);
        Assert.Equal(DefaultUser, ids[0]);
    }

    [Fact]
    public async Task ResolveRecipientIdsAsync_returns_empty_when_unresolvable()
    {
        var identity = new FakeIdentityClient(TenantId, []);
        var resolver = CreateResolver(identity, new NotificationRecipientOptions());

        var ids = await resolver.ResolveRecipientIdsAsync(null, null, null, CancellationToken.None);

        Assert.Empty(ids);
    }

    [Fact]
    public async Task ResolveRecipientIdsAsync_uses_envelope_tenant_when_event_tenant_missing()
    {
        var identity = new FakeIdentityClient(TenantId, [Sindico]);
        var resolver = CreateResolver(identity, new NotificationRecipientOptions());

        var ids = await resolver.ResolveRecipientIdsAsync(null, null, TenantId, CancellationToken.None);

        Assert.Single(ids);
        Assert.Equal(Sindico, ids[0]);
        Assert.Equal(TenantId, identity.LastTenantId);
    }

    private static AlertNotificationRecipientResolver CreateResolver(
        IIdentityNotificationRecipientClient identity,
        NotificationRecipientOptions options) =>
        new(options, identity, NullLogger<AlertNotificationRecipientResolver>.Instance);

    private sealed class FakeIdentityClient : IIdentityNotificationRecipientClient
    {
        private readonly Guid _tenantId;
        private readonly IReadOnlyList<Guid> _userIds;

        public FakeIdentityClient(Guid tenantId, IReadOnlyList<Guid> userIds)
        {
            _tenantId = tenantId;
            _userIds = userIds;
        }

        public Guid? LastTenantId { get; private set; }

        public Task<IReadOnlyList<Guid>> GetGovernanceRecipientUserIdsAsync(Guid tenantId, CancellationToken ct)
        {
            LastTenantId = tenantId;
            return Task.FromResult(tenantId == _tenantId ? _userIds : Array.Empty<Guid>());
        }
    }
}
