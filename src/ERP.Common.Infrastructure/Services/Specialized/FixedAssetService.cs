using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.Specialized;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.Specialized;
using ERP.Common.Domain.Entities.FixedAssets;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Specialized;

public class FixedAssetService : IFixedAssetService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;

    public FixedAssetService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
    }

    private int GetCompanyId() => _currentUserContext.CompanyId ?? 1;

    public async Task<IReadOnlyList<AssetGroupDto>> GetAssetGroupsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var groups = await _dbContext.AssetGroups
            .AsNoTracking()
            .Where(g => g.CompanyId == companyId)
            .OrderBy(g => g.GroupCode)
            .Select(g => new AssetGroupDto(
                g.Id,
                g.CompanyId,
                g.GroupCode,
                g.GroupName,
                g.DepreciationRate,
                g.GlAssetAccountId,
                g.GlAccumDeprAccountId,
                g.GlDeprExpenseAccountId
            ))
            .ToListAsync(cancellationToken);

        return groups;
    }

    public async Task<AssetGroupDto> CreateAssetGroupAsync(CreateAssetGroupRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = request.GroupCode.Trim().ToUpperInvariant();

        var exists = await _dbContext.AssetGroups.AnyAsync(g => g.CompanyId == companyId && g.GroupCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Asset group with code '{code}' already exists.");
        }

        var group = new AssetGroup
        {
            CompanyId = companyId,
            GroupCode = code,
            GroupName = request.GroupName.Trim(),
            DepreciationRate = request.DepreciationRate,
            GlAssetAccountId = request.GlAssetAccountId,
            GlAccumDeprAccountId = request.GlAccumDeprAccountId,
            GlDeprExpenseAccountId = request.GlDeprExpenseAccountId
        };

        _dbContext.AssetGroups.Add(group);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AssetGroupDto(group.Id, group.CompanyId, group.GroupCode, group.GroupName, group.DepreciationRate, group.GlAssetAccountId, group.GlAccumDeprAccountId, group.GlDeprExpenseAccountId);
    }

    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(int? assetGroupId = null, bool? isDisposed = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from a in _dbContext.Assets.AsNoTracking()
                    where a.CompanyId == companyId
                    join g in _dbContext.AssetGroups.AsNoTracking() on a.AssetGroupId equals g.Id
                    join jv in _dbContext.JournalVouchers.AsNoTracking() on a.DisposalJournalVoucherId equals jv.Id into jvJoin
                    from jv in jvJoin.DefaultIfEmpty()
                    select new { a, GroupName = g.GroupName, JvNumber = jv != null ? jv.VoucherNumber : null };

        if (assetGroupId.HasValue)
        {
            query = query.Where(x => x.a.AssetGroupId == assetGroupId.Value);
        }

        if (isDisposed.HasValue)
        {
            query = query.Where(x => x.a.IsDisposed == isDisposed.Value);
        }

        var list = await query.OrderBy(x => x.a.AssetCode).ToListAsync(cancellationToken);

        return list.Select(x => MapToDto(x.a, x.GroupName, x.JvNumber)).ToList();
    }

    public async Task<AssetDto?> GetAssetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var item = await (from a in _dbContext.Assets.AsNoTracking()
                          where a.CompanyId == companyId && a.Id == id
                          join g in _dbContext.AssetGroups.AsNoTracking() on a.AssetGroupId equals g.Id
                          join jv in _dbContext.JournalVouchers.AsNoTracking() on a.DisposalJournalVoucherId equals jv.Id into jvJoin
                          from jv in jvJoin.DefaultIfEmpty()
                          select new { a, GroupName = g.GroupName, JvNumber = jv != null ? jv.VoucherNumber : null })
                         .FirstOrDefaultAsync(cancellationToken);

        return item == null ? null : MapToDto(item.a, item.GroupName, item.JvNumber);
    }

    public async Task<AssetDto> CreateAssetAsync(CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = request.AssetCode.Trim().ToUpperInvariant();

        var exists = await _dbContext.Assets.AnyAsync(a => a.CompanyId == companyId && a.AssetCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Asset with code '{code}' already exists.");
        }

        var group = await _dbContext.AssetGroups.FirstOrDefaultAsync(g => g.CompanyId == companyId && g.Id == request.AssetGroupId, cancellationToken);
        if (group == null)
        {
            throw new InvalidOperationException($"Asset Group with ID {request.AssetGroupId} was not found.");
        }

        var asset = new Asset
        {
            CompanyId = companyId,
            AssetCode = code,
            AssetName = request.AssetName.Trim(),
            AssetGroupId = request.AssetGroupId,
            PurchaseDate = request.PurchaseDate.Date,
            PurchaseCost = request.PurchaseCost,
            CurrentBookValue = request.PurchaseCost,
            AccumulatedDepreciation = 0m,
            SalvageValue = request.SalvageValue,
            UsefulLifeMonths = request.UsefulLifeMonths > 0 ? request.UsefulLifeMonths : 60,
            DepreciationMethod = string.IsNullOrWhiteSpace(request.DepreciationMethod) ? "StraightLine" : request.DepreciationMethod.Trim(),
            IsDisposed = false
        };

        _dbContext.Assets.Add(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetAssetByIdAsync(asset.Id, cancellationToken))!;
    }

    public async Task<DepreciationRunResultDto> RunDepreciationAsync(RunDepreciationRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var periodDate = request.PeriodDate.Date;
        var year = periodDate.Year;
        var month = periodDate.Month;

        var assets = await _dbContext.Assets
            .Include(a => a.AssetGroup)
            .Where(a => a.CompanyId == companyId && !a.IsDisposed && a.PurchaseDate <= periodDate)
            .ToListAsync(cancellationToken);

        if (assets.Count == 0)
        {
            throw new InvalidOperationException("No eligible active fixed assets found for depreciation run.");
        }

        var entries = new List<DepreciationEntry>();
        var entryDtos = new List<DepreciationEntryDto>();
        decimal totalDepreciation = 0m;

        // Group by expense and accumulated depreciation accounts for GL posting
        var glLinesMap = new Dictionary<(int ExpenseId, int AccumId), decimal>();

        foreach (var asset in assets)
        {
            var rate = asset.AssetGroup?.DepreciationRate ?? 10m;
            var deprAmount = asset.CalculateMonthlyDepreciation(rate);

            if (deprAmount <= 0) continue;

            asset.AccumulatedDepreciation += deprAmount;
            asset.CurrentBookValue = Math.Max(asset.SalvageValue, asset.CurrentBookValue - deprAmount);

            var entry = new DepreciationEntry
            {
                CompanyId = companyId,
                AssetId = asset.Id,
                ScheduleDate = periodDate,
                Year = year,
                Month = month,
                DepreciationAmount = deprAmount,
                AccumulatedDepreciationAfter = asset.AccumulatedDepreciation,
                BookValueAfter = asset.CurrentBookValue,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUserContext.UserName ?? "System"
            };

            entries.Add(entry);
            entryDtos.Add(new DepreciationEntryDto(
                asset.Id,
                asset.AssetCode,
                asset.AssetName,
                deprAmount,
                asset.AccumulatedDepreciation,
                asset.CurrentBookValue
            ));

            totalDepreciation += deprAmount;

            int expAcctId = asset.AssetGroup!.GlDeprExpenseAccountId;
            int accumAcctId = asset.AssetGroup!.GlAccumDeprAccountId;
            var key = (expAcctId, accumAcctId);

            if (!glLinesMap.ContainsKey(key)) glLinesMap[key] = 0m;
            glLinesMap[key] += deprAmount;
        }

        if (totalDepreciation <= 0)
        {
            throw new InvalidOperationException("Calculated depreciation amount is zero across all assets.");
        }

        // Generate Double-Entry Journal Voucher
        var jvLines = new List<CreateVoucherLineRequest>();
        foreach (var kvp in glLinesMap)
        {
            var expId = kvp.Key.ExpenseId;
            var accumId = kvp.Key.AccumId;
            var amount = kvp.Value;

            jvLines.Add(new(
                AccountId: expId,
                Debit: amount,
                Credit: 0m,
                Particulars: $"Fixed Asset Monthly Depreciation Run for {periodDate:yyyy-MM}"
            ));

            jvLines.Add(new(
                AccountId: accumId,
                Debit: 0m,
                Credit: amount,
                Particulars: $"Accumulated Depreciation for {periodDate:yyyy-MM}"
            ));
        }

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(periodDate),
            Reference: $"DEPR-{year}{month:D2}",
            Description: $"Fixed Assets Monthly Depreciation: {periodDate:MMMM yyyy} (Assets: {entries.Count})",
            FiscalPeriodId: request.FiscalPeriodId,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create depreciation journal voucher: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post depreciation journal voucher: {postResult.Error}");
        }

        foreach (var e in entries)
        {
            e.JournalVoucherId = createResult.Value!.Id;
        }

        _dbContext.DepreciationEntries.AddRange(entries);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DepreciationRunResultDto(
            TotalAssetsProcessed: entries.Count,
            TotalDepreciationAmount: totalDepreciation,
            JournalVoucherId: createResult.Value!.Id,
            JournalVoucherNumber: createResult.Value!.VoucherNumber,
            Entries: entryDtos
        );
    }

    public async Task<AssetDisposalResultDto> DisposeAssetAsync(DisposeAssetRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var asset = await _dbContext.Assets
            .Include(a => a.AssetGroup)
            .FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == request.AssetId, cancellationToken);

        if (asset == null)
        {
            throw new InvalidOperationException($"Asset with ID {request.AssetId} was not found.");
        }

        if (asset.IsDisposed)
        {
            throw new InvalidOperationException($"Asset '{asset.AssetCode}' is already disposed on {asset.DisposalDate:yyyy-MM-dd}.");
        }

        var group = asset.AssetGroup;
        if (group == null)
        {
            group = await _dbContext.AssetGroups.FirstAsync(g => g.Id == asset.AssetGroupId, cancellationToken);
        }

        var disposalDate = request.DisposalDate.Date;
        var proceeds = request.DisposalProceeds;
        var cost = asset.PurchaseCost;
        var accumDepr = asset.AccumulatedDepreciation;
        var bookValue = asset.CurrentBookValue;

        // Gain / Loss calculation: Proceeds - BookValue
        var gainLoss = proceeds - bookValue;
        bool isGain = gainLoss >= 0m;
        var gainLossAbs = Math.Abs(gainLoss);

        // Bank / Cash Account (default Prime Bank: 1120)
        var depositAccountId = request.ProceedsDepositAccountId ?? 3; // Account 1120 Prime Bank
        var depositAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && (a.Id == depositAccountId || a.AccountCode == "1120"), cancellationToken);
        int finalDepositAcctId = depositAccount?.Id ?? depositAccountId;

        // GL Gain / Loss Account:
        // Gain: 4200 (Other Operating Income)
        // Loss: 5120 (Inventory / Asset Adjustment Variance)
        var gainAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "4200", cancellationToken);
        var lossAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "5120", cancellationToken);

        int gainAccountId = gainAccount?.Id ?? 17;
        int lossAccountId = lossAccount?.Id ?? 26;

        // Double-entry GL Posting:
        // Dr: Bank (Proceeds)
        // Dr: Accumulated Depreciation (accumDepr)
        // Cr: Fixed Asset Cost Account (cost)
        // If Gain: Cr 4200 Gain on Disposal (gainLoss)
        // If Loss: Dr 5120 Loss on Disposal (gainLossAbs)
        var jvLines = new List<CreateVoucherLineRequest>();

        if (proceeds > 0)
        {
            jvLines.Add(new(
                AccountId: finalDepositAcctId,
                Debit: proceeds,
                Credit: 0m,
                Particulars: $"Proceeds from disposal of {asset.AssetCode} ({asset.AssetName})"
            ));
        }

        if (accumDepr > 0)
        {
            jvLines.Add(new(
                AccountId: group.GlAccumDeprAccountId,
                Debit: accumDepr,
                Credit: 0m,
                Particulars: $"Clear accumulated depreciation on disposal of {asset.AssetCode}"
            ));
        }

        if (!isGain && gainLossAbs > 0)
        {
            jvLines.Add(new(
                AccountId: lossAccountId,
                Debit: gainLossAbs,
                Credit: 0m,
                Particulars: $"Loss on disposal of {asset.AssetCode}"
            ));
        }

        // Cr: Asset original cost
        jvLines.Add(new(
            AccountId: group.GlAssetAccountId,
            Debit: 0m,
            Credit: cost,
            Particulars: $"Deregister asset cost on disposal of {asset.AssetCode}"
        ));

        if (isGain && gainLossAbs > 0)
        {
            jvLines.Add(new(
                AccountId: gainAccountId,
                Debit: 0m,
                Credit: gainLossAbs,
                Particulars: $"Gain on disposal of {asset.AssetCode}"
            ));
        }

        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(disposalDate),
            Reference: request.ReferenceNumber ?? $"DISP-{asset.AssetCode}",
            Description: $"Fixed Asset Disposal: {asset.AssetCode} - {asset.AssetName} (Proceeds: {proceeds:C}, BookValue: {bookValue:C})",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create asset disposal journal voucher: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post asset disposal journal voucher: {postResult.Error}");
        }

        asset.IsDisposed = true;
        asset.DisposalDate = disposalDate;
        asset.DisposalAmount = proceeds;
        asset.GainLossAmount = gainLoss;
        asset.DisposalJournalVoucherId = createResult.Value!.Id;
        asset.CurrentBookValue = 0m;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AssetDisposalResultDto(
            AssetId: asset.Id,
            AssetCode: asset.AssetCode,
            AssetName: asset.AssetName,
            DisposalDate: disposalDate,
            PurchaseCost: cost,
            AccumulatedDepreciation: accumDepr,
            BookValueAtDisposal: bookValue,
            DisposalProceeds: proceeds,
            GainLossAmount: gainLoss,
            IsGain: isGain,
            JournalVoucherId: createResult.Value!.Id,
            JournalVoucherNumber: createResult.Value!.VoucherNumber
        );
    }

    public async Task<AssetScheduleReportDto> GetAssetScheduleReportAsync(DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var cutoff = asOfDate?.Date ?? DateTime.UtcNow.Date;

        var assets = await _dbContext.Assets
            .Include(a => a.AssetGroup)
            .Include(a => a.DepreciationEntries)
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.PurchaseDate <= cutoff)
            .OrderBy(a => a.AssetCode)
            .ToListAsync(cancellationToken);

        var items = new List<AssetScheduleItemDto>();
        decimal totalCost = 0m;
        decimal totalAccum = 0m;
        decimal totalNetBook = 0m;

        foreach (var a in assets)
        {
            var deprBeforeCutoff = a.DepreciationEntries.Where(e => e.ScheduleDate <= cutoff).Sum(e => e.DepreciationAmount);
            var currentNet = a.IsDisposed ? 0m : Math.Max(0m, a.PurchaseCost - deprBeforeCutoff);

            items.Add(new AssetScheduleItemDto(
                AssetId: a.Id,
                AssetCode: a.AssetCode,
                AssetName: a.AssetName,
                GroupName: a.AssetGroup?.GroupName ?? string.Empty,
                PurchaseCost: a.PurchaseCost,
                PriorAccumulatedDepreciation: deprBeforeCutoff,
                CurrentPeriodDepreciation: 0m,
                TotalAccumulatedDepreciation: deprBeforeCutoff,
                NetBookValue: currentNet
            ));

            totalCost += a.PurchaseCost;
            totalAccum += deprBeforeCutoff;
            totalNetBook += currentNet;
        }

        return new AssetScheduleReportDto(
            AsOfDate: cutoff,
            TotalPurchaseCost: totalCost,
            TotalAccumulatedDepreciation: totalAccum,
            TotalNetBookValue: totalNetBook,
            Assets: items
        );
    }

    private static AssetDto MapToDto(Asset a, string groupName, string? jvNumber)
    {
        return new AssetDto(
            a.Id,
            a.CompanyId,
            a.AssetCode,
            a.AssetName,
            a.AssetGroupId,
            groupName,
            a.PurchaseDate,
            a.PurchaseCost,
            a.CurrentBookValue,
            a.AccumulatedDepreciation,
            a.SalvageValue,
            a.UsefulLifeMonths,
            a.DepreciationMethod,
            a.IsDisposed,
            a.DisposalDate,
            a.DisposalAmount,
            a.GainLossAmount,
            a.DisposalJournalVoucherId,
            jvNumber
        );
    }
}
