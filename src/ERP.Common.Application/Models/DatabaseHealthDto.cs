namespace ERP.Common.Application.Models;

public record DatabaseHealthDto(
    bool IsConnected,
    string DatabaseName,
    string ServerVersion,
    DateTime ServerUtcTime,
    string CompatibilityLevel,
    string Collation,
    string? ErrorMessage = null
);
