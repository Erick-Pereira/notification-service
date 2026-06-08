namespace Simcag.NotificationService.Application.Abstractions;

/// <summary>Utilizadores do condomínio elegíveis a receber alertas (identity-service).</summary>
public interface IIdentityNotificationRecipientClient
{
    Task<IReadOnlyList<Guid>> GetGovernanceRecipientUserIdsAsync(Guid tenantId, CancellationToken ct);
}
