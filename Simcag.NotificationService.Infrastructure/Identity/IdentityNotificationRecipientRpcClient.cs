using Microsoft.Extensions.Logging;
using Simcag.NotificationService.Application.Abstractions;
using Simcag.Shared.Messaging.Rpc;
using Simcag.Shared.Messaging.Rpc.Contracts;

namespace Simcag.NotificationService.Infrastructure.Identity;

public sealed class IdentityNotificationRecipientRpcClient : IIdentityNotificationRecipientClient
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly IRabbitMqRpcClient _rpc;
    private readonly ILogger<IdentityNotificationRecipientRpcClient> _logger;

    public IdentityNotificationRecipientRpcClient(
        IRabbitMqRpcClient rpc,
        ILogger<IdentityNotificationRecipientRpcClient> logger)
    {
        _rpc = rpc;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Guid>> GetGovernanceRecipientUserIdsAsync(Guid tenantId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty)
            return Array.Empty<Guid>();

        try
        {
            var response = await _rpc.RequestAsync<GetNotificationRecipientsRpcRequest, GetNotificationRecipientsRpcResponse>(
                RpcQueues.IdentityGetNotificationRecipients,
                new GetNotificationRecipientsRpcRequest { TenantId = tenantId },
                DefaultTimeout,
                ct);

            if (response?.UserIds is null || response.UserIds.Count == 0)
                return Array.Empty<Guid>();

            return response.UserIds.Where(id => id != Guid.Empty).Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RPC identity.get-notification-recipients failed for tenant {TenantId}", tenantId);
            return Array.Empty<Guid>();
        }
    }
}
