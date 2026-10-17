using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Domain.Entities.SystemAdmin;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Common;

public class DocumentSequenceService : IDocumentSequenceService
{
    private readonly ErpDbContext _dbContext;
    private static readonly object _syncLock = new();

    private static readonly Dictionary<string, (string DefaultPrefix, int DefaultLength)> StandardEntityDefaults =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Product"] = ("PRD-", 6),
            ["ProductMaster"] = ("PRD-", 6),
            ["Customer"] = ("CUS-", 6),
            ["Vendor"] = ("VEN-", 6),
            ["Account"] = ("ACC-", 6),
            ["Warehouse"] = ("WH-", 4),
            ["Employee"] = ("EMP-", 4),
            ["FixedAsset"] = ("FA-", 4),
            ["Asset"] = ("FA-", 4),
            ["BillOfMaterials"] = ("BOM-", 4),
            ["BOM"] = ("BOM-", 4),
            ["JournalVoucher"] = ("JV-", 4),
            ["JV"] = ("JV-", 4),
            ["SalesOrder"] = ("SO-", 4),
            ["PurchaseOrder"] = ("PO-", 4),
            ["DeliveryChallan"] = ("DC-", 4),
            ["Invoice"] = ("INV-", 4),
            ["PurchaseRequisition"] = ("PR-", 4),
            ["StockTransfer"] = ("IWT-", 4),
            ["BankAccount"] = ("BNK-", 4)
        };

    public DocumentSequenceService(ErpDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetNextNumberAsync(
        int companyId,
        string documentType,
        string? prefix = null,
        string? fiscalYear = null,
        int numberLength = 4,
        CancellationToken cancellationToken = default)
    {
        var fy = string.IsNullOrWhiteSpace(fiscalYear) ? "GLOBAL" : fiscalYear;
        var pfx = prefix ?? ResolveDefaultPrefix(documentType);
        var len = numberLength > 0 ? numberLength : ResolveDefaultLength(documentType);

        if (_dbContext.Database.IsSqlServer())
        {
            try
            {
                var companyIdParam = new SqlParameter("@CompanyId", SqlDbType.Int) { Value = companyId };
                var docTypeParam = new SqlParameter("@DocumentType", SqlDbType.NVarChar, 50) { Value = documentType };
                var prefixParam = new SqlParameter("@Prefix", SqlDbType.NVarChar, 20) { Value = (object?)pfx ?? DBNull.Value };
                var fyParam = new SqlParameter("@FiscalYear", SqlDbType.NVarChar, 10) { Value = (object?)fy ?? DBNull.Value };
                var lenParam = new SqlParameter("@NumberLength", SqlDbType.Int) { Value = len };
                var outputParam = new SqlParameter("@GeneratedNumber", SqlDbType.NVarChar, 50)
                {
                    Direction = ParameterDirection.Output
                };

                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC sa.usp_GetNextDocumentNumber @CompanyId, @DocumentType, @Prefix, @FiscalYear, @NumberLength, @GeneratedNumber OUTPUT",
                    new object[] { companyIdParam, docTypeParam, prefixParam, fyParam, lenParam, outputParam },
                    cancellationToken);

                if (outputParam.Value != null && !Convert.IsDBNull(outputParam.Value))
                {
                    return outputParam.Value.ToString()!;
                }
            }
            catch
            {
                // Fall back to direct concurrency-safe sequence table mutation
            }
        }

        return await GenerateAtomicSequenceAsync(companyId, documentType, pfx, fy, len, cancellationToken);
    }

    public async Task<string> GenerateCodeAsync(
        int companyId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigurationAsync(companyId, entityType, cancellationToken);
        if (!config.IsActive)
        {
            throw new InvalidOperationException($"Sequence code generation for entity '{entityType}' is currently inactive.");
        }

        return await GetNextNumberAsync(
            companyId,
            config.EntityType,
            config.Prefix,
            config.FiscalYear,
            config.NumberLength,
            cancellationToken);
    }

    public async Task<DocumentSequenceConfigDto> GetConfigurationAsync(
        int companyId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        var seq = await _dbContext.DocumentSequences
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.CompanyId == companyId &&
                                      d.DocumentType.ToLower() == entityType.ToLower() &&
                                      !d.IsDeleted, cancellationToken);

        if (seq != null)
        {
            return new DocumentSequenceConfigDto
            {
                EntityType = seq.DocumentType,
                Prefix = seq.Prefix,
                NumberLength = seq.NumberLength,
                FiscalYear = seq.FiscalYear,
                CurrentNumber = seq.CurrentNumber,
                IsActive = seq.IsActive,
                NextPreviewCode = $"{seq.Prefix}{(seq.CurrentNumber + 1).ToString($"D{seq.NumberLength}")}"
            };
        }

        var defaultPrefix = ResolveDefaultPrefix(entityType);
        var defaultLen = ResolveDefaultLength(entityType);

        return new DocumentSequenceConfigDto
        {
            EntityType = entityType,
            Prefix = defaultPrefix,
            NumberLength = defaultLen,
            FiscalYear = "GLOBAL",
            CurrentNumber = 0,
            IsActive = true,
            NextPreviewCode = $"{defaultPrefix}{1.ToString($"D{defaultLen}")}"
        };
    }

    public async Task<IReadOnlyList<DocumentSequenceConfigDto>> GetAllConfigurationsAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        var dbSeqs = await _dbContext.DocumentSequences
            .IgnoreQueryFilters()
            .Where(d => d.CompanyId == companyId && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        var result = new List<DocumentSequenceConfigDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var seq in dbSeqs)
        {
            seen.Add(seq.DocumentType);
            result.Add(new DocumentSequenceConfigDto
            {
                EntityType = seq.DocumentType,
                Prefix = seq.Prefix,
                NumberLength = seq.NumberLength,
                FiscalYear = seq.FiscalYear,
                CurrentNumber = seq.CurrentNumber,
                IsActive = seq.IsActive,
                NextPreviewCode = $"{seq.Prefix}{(seq.CurrentNumber + 1).ToString($"D{seq.NumberLength}")}"
            });
        }

        foreach (var kvp in StandardEntityDefaults)
        {
            if (!seen.Contains(kvp.Key))
            {
                result.Add(new DocumentSequenceConfigDto
                {
                    EntityType = kvp.Key,
                    Prefix = kvp.Value.DefaultPrefix,
                    NumberLength = kvp.Value.DefaultLength,
                    FiscalYear = "GLOBAL",
                    CurrentNumber = 0,
                    IsActive = true,
                    NextPreviewCode = $"{kvp.Value.DefaultPrefix}{1.ToString($"D{kvp.Value.DefaultLength}")}"
                });
            }
        }

        return result;
    }

    public async Task SetConfigurationAsync(
        int companyId,
        string entityType,
        string prefix,
        int numberLength,
        string? fiscalYear = "GLOBAL",
        bool isActive = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Business code prefix cannot be empty.", nameof(prefix));
        }

        if (numberLength < 3 || numberLength > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(numberLength), "Number length must be between 3 and 12 digits.");
        }

        var fy = string.IsNullOrWhiteSpace(fiscalYear) ? "GLOBAL" : fiscalYear.Trim();

        var seq = await _dbContext.DocumentSequences
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.CompanyId == companyId &&
                                      d.DocumentType.ToLower() == entityType.ToLower() &&
                                      d.FiscalYear == fy &&
                                      !d.IsDeleted, cancellationToken);

        if (seq == null)
        {
            seq = new DocumentSequence
            {
                CompanyId = companyId,
                DocumentType = entityType.Trim(),
                Prefix = prefix.Trim(),
                FiscalYear = fy,
                NumberLength = numberLength,
                CurrentNumber = 0,
                IsActive = isActive,
                CreatedAtUtc = DateTime.UtcNow
            };
            _dbContext.DocumentSequences.Add(seq);
        }
        else
        {
            seq.Prefix = prefix.Trim();
            seq.NumberLength = numberLength;
            seq.IsActive = isActive;
            seq.ModifiedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateAtomicSequenceAsync(
        int companyId,
        string documentType,
        string prefix,
        string fiscalYear,
        int numberLength,
        CancellationToken cancellationToken)
    {
        lock (_syncLock)
        {
            // Thread-safe in-memory and EF Core execution
            var seq = _dbContext.DocumentSequences
                .IgnoreQueryFilters()
                .FirstOrDefault(d => d.CompanyId == companyId &&
                                     d.DocumentType.ToLower() == documentType.ToLower() &&
                                     d.FiscalYear == fiscalYear &&
                                     !d.IsDeleted);

            if (seq == null)
            {
                seq = new DocumentSequence
                {
                    CompanyId = companyId,
                    DocumentType = documentType.Trim(),
                    Prefix = prefix,
                    FiscalYear = fiscalYear,
                    NumberLength = numberLength,
                    CurrentNumber = 1,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _dbContext.DocumentSequences.Add(seq);
            }
            else
            {
                seq.CurrentNumber++;
                seq.Prefix = prefix;
                seq.NumberLength = numberLength;
                seq.ModifiedAtUtc = DateTime.UtcNow;
            }

            _dbContext.SaveChanges();

            return $"{seq.Prefix}{seq.CurrentNumber.ToString($"D{seq.NumberLength}")}";
        }
    }

    private static string ResolveDefaultPrefix(string entityType)
    {
        if (StandardEntityDefaults.TryGetValue(entityType, out var val))
        {
            return val.DefaultPrefix;
        }

        var clean = entityType.Trim().ToUpperInvariant();
        return clean.Length <= 4 ? $"{clean}-" : $"{clean[..3]}-";
    }

    private static int ResolveDefaultLength(string entityType)
    {
        if (StandardEntityDefaults.TryGetValue(entityType, out var val))
        {
            return val.DefaultLength;
        }

        return 6;
    }
}
