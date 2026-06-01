namespace Simcag.NotificationService.Domain.ValueObjects;

/// <summary>
/// Tipos de alerta conforme TCC_ECONDOMIZA.docx.
/// </summary>
public static class AlertType
{
    /// <summary>Sobrepreço em relação à média de mercado.</summary>
    public const string OverpriceMarket = "OVERPRICE_MARKET";
    
    /// <summary>Escalada de preço com fornecedor (aumento repetido).</summary>
    public const string SupplierEscalation = "SUPPLIER_ESCALATION";
    
    /// <summary>Dependência excessiva de único fornecedor.</summary>
    public const string SupplierDependency = "SUPPLIER_DEPENDENCY";
    
    /// <summary>Não conformidade documental (falta de nota fiscal, etc.).</summary>
    public const string DocumentNonCompliance = "DOCUMENT_NON_COMPLIANCE";
    
    /// <summary>Desvio de categoria de despesa.</summary>
    public const string CategoryDeviation = "CATEGORY_DEVIATION";

    private static readonly Dictionary<string, int> SeverityMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        [OverpriceMarket] = 2,      // CRITICAL
        [SupplierEscalation] = 1,   // WARNING
        [SupplierDependency] = 1,   // WARNING
        [DocumentNonCompliance] = 2, // CRITICAL
        [CategoryDeviation] = 0     // Info
    };

    public static int GetSeverity(string alertType) => 
        SeverityMapping.TryGetValue(alertType, out var severity) ? severity : 0;
}
