using InteAcc.Common.Application.Models.Migration;
using Xunit;

namespace InteAcc.UnitTests;

public class DataMigrationAndParityAuditTests
{
    [Fact]
    public void GeneralLedgerParity_ShouldBeBalanced_WhenDebitsEqualCredits()
    {
        // Arrange
        var glParity = new GeneralLedgerParityDto
        {
            TotalDebits = 9754654.6200m,
            TotalCredits = 9754654.6200m,
            Variance = 0.0000m,
            TotalPostedVouchers = 21,
            TotalVoucherLines = 57
        };

        // Assert
        Assert.True(glParity.IsBalanced);
        Assert.Equal(0.0000m, glParity.Variance);
    }

    [Fact]
    public void GeneralLedgerParity_ShouldDetectImbalance_WhenDebitsDoNotEqualCredits()
    {
        // Arrange
        var glParity = new GeneralLedgerParityDto
        {
            TotalDebits = 9754654.6200m,
            TotalCredits = 9754600.0000m,
            Variance = 54.6200m
        };

        // Assert
        Assert.False(glParity.IsBalanced);
        Assert.NotEqual(0.0000m, glParity.Variance);
    }

    [Fact]
    public void FinancialParityAuditReport_ShouldAchieveSystemParity_WhenAllSubledgersBalanced()
    {
        // Arrange
        var report = new FinancialParityAuditReport
        {
            CompanyId = 1,
            GeneralLedgerParity = new GeneralLedgerParityDto
            {
                TotalDebits = 9754654.6200m,
                TotalCredits = 9754654.6200m,
                Variance = 0.0000m
            },
            SubledgerReconciliations = new List<SubledgerParityItem>
            {
                new() { SubledgerName = "AR", ControlAccountCodes = "1130", ControlAccountBalance = 40000m, SubledgerTotal = 40000m, Variance = 0m },
                new() { SubledgerName = "AP", ControlAccountCodes = "2110", ControlAccountBalance = 8000m, SubledgerTotal = 8000m, Variance = 0m },
                new() { SubledgerName = "Inventory", ControlAccountCodes = "1140, 1145", ControlAccountBalance = 870000m, SubledgerTotal = 870000m, Variance = 0m },
                new() { SubledgerName = "Bank & Cash", ControlAccountCodes = "1111, 1120", ControlAccountBalance = 7730757.69m, SubledgerTotal = 7730757.69m, Variance = 0m },
                new() { SubledgerName = "Real Estate", ControlAccountCodes = "2150", ControlAccountBalance = 7800000m, SubledgerTotal = 7800000m, Variance = 0m },
                new() { SubledgerName = "Fixed Assets", ControlAccountCodes = "1290", ControlAccountBalance = 7500m, SubledgerTotal = 7500m, Variance = 0m }
            }
        };

        // Assert
        Assert.True(report.IsSystemParityAchieved);
        Assert.Equal(0, report.CriticalDiscrepanciesCount);
    }

    [Fact]
    public void FinancialParityAuditReport_ShouldDetectDiscrepancy_WhenSubledgerVarianceExists()
    {
        // Arrange
        var report = new FinancialParityAuditReport
        {
            CompanyId = 1,
            GeneralLedgerParity = new GeneralLedgerParityDto
            {
                TotalDebits = 1000m,
                TotalCredits = 1000m,
                Variance = 0m
            },
            SubledgerReconciliations = new List<SubledgerParityItem>
            {
                new() { SubledgerName = "AR", ControlAccountCodes = "1130", ControlAccountBalance = 40000m, SubledgerTotal = 39000m, Variance = 1000m }
            }
        };

        // Assert
        Assert.False(report.IsSystemParityAchieved);
        Assert.Equal(1, report.CriticalDiscrepanciesCount);
    }

    [Fact]
    public void CutoverChecklist_OverallReady_RequiresAllGatesPassing()
    {
        // Arrange
        var checklist = new CutoverChecklistResponse
        {
            DatabaseConnectivityVerified = true,
            FiscalYearAndPeriodOpen = true,
            TrialBalanceVarianceZero = true,
            AllSubledgersReconciled = true,
            NoUnpostedDraftVouchers = true,
            RbacAndSecurityConfigured = true,
            BackupSystemReady = true
        };

        // Assert
        Assert.True(checklist.OverallCutoverReady);

        // Fails if even one gate is false
        checklist.AllSubledgersReconciled = false;
        Assert.False(checklist.OverallCutoverReady);
    }

    [Fact]
    public void EtlMigrationSummary_ShouldAggregateTotalsAccurately()
    {
        // Arrange
        var summary = new EtlMigrationSummaryDto
        {
            BatchNumber = "TEST-BATCH-001",
            ModuleSummaries = new List<ModuleMigrationSummaryDto>
            {
                new() { SourceModule = "gl", RecordsExtracted = 29, RecordsMigrated = 29, RecordsFailed = 0 },
                new() { SourceModule = "ar", RecordsExtracted = 4, RecordsMigrated = 4, RecordsFailed = 0 },
                new() { SourceModule = "ap", RecordsExtracted = 3, RecordsMigrated = 3, RecordsFailed = 0 }
            }
        };
        summary.TotalExtracted = summary.ModuleSummaries.Sum(m => m.RecordsExtracted);
        summary.TotalMigrated = summary.ModuleSummaries.Sum(m => m.RecordsMigrated);
        summary.TotalFailed = summary.ModuleSummaries.Sum(m => m.RecordsFailed);

        // Assert
        Assert.Equal(36, summary.TotalExtracted);
        Assert.Equal(36, summary.TotalMigrated);
        Assert.Equal(0, summary.TotalFailed);
        Assert.True(summary.IsSuccess);
    }
}
