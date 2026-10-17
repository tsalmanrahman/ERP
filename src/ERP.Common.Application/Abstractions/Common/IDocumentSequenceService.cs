namespace ERP.Common.Application.Abstractions.Common;

public class DocumentSequenceConfigDto
{
    public string EntityType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int NumberLength { get; set; } = 6;
    public string FiscalYear { get; set; } = "GLOBAL";
    public long CurrentNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string NextPreviewCode { get; set; } = string.Empty;
}

public interface IDocumentSequenceService
{
    Task<string> GetNextNumberAsync(
        int companyId,
        string documentType,
        string? prefix = null,
        string? fiscalYear = null,
        int numberLength = 4,
        CancellationToken cancellationToken = default);

    Task<string> GenerateCodeAsync(
        int companyId,
        string entityType,
        CancellationToken cancellationToken = default);

    Task<DocumentSequenceConfigDto> GetConfigurationAsync(
        int companyId,
        string entityType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentSequenceConfigDto>> GetAllConfigurationsAsync(
        int companyId,
        CancellationToken cancellationToken = default);

    Task SetConfigurationAsync(
        int companyId,
        string entityType,
        string prefix,
        int numberLength,
        string? fiscalYear = "GLOBAL",
        bool isActive = true,
        CancellationToken cancellationToken = default);
}
