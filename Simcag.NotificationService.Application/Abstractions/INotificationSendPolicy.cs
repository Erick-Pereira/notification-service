namespace Simcag.NotificationService.Application.Abstractions;

/// <summary>
/// Dedup + rate limit antes de chamar o provedor (Redis).
/// </summary>
public interface INotificationSendPolicy
{
    /// <param name="deduplicationKey">Chave lógica idempotente (ex.: user + alerta + canal).</param>
    Task<bool> TryAcquireAsync(string deduplicationKey, string rateLimitKey, CancellationToken cancellationToken = default);
}
