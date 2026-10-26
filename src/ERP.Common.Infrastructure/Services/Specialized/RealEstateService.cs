using Microsoft.EntityFrameworkCore;
using ERP.Common.Application.Abstractions.Common;
using ERP.Common.Application.Abstractions.Finance;
using ERP.Common.Application.Abstractions.Specialized;
using ERP.Common.Application.Models.Finance;
using ERP.Common.Application.Models.Specialized;
using ERP.Common.Domain.Entities.RealEstate;
using ERP.Common.Infrastructure.Data;

namespace ERP.Common.Infrastructure.Services.Specialized;

public class RealEstateService : IRealEstateService
{
    private readonly ErpDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IJournalVoucherService _journalVoucherService;

    public RealEstateService(
        ErpDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IJournalVoucherService journalVoucherService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _journalVoucherService = journalVoucherService;
    }

    private int GetCompanyId() => _currentUserContext.GetRequiredCompanyId();

    public async Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.ProjectCode)
            .Select(p => new ProjectDto(
                p.Id,
                p.CompanyId,
                p.ProjectCode,
                p.ProjectName,
                p.Location,
                p.TotalUnits,
                p.EstimatedCost,
                p.Status
            ))
            .ToListAsync(cancellationToken);

        return projects;
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var code = request.ProjectCode.Trim().ToUpperInvariant();

        var exists = await _dbContext.Projects.AnyAsync(p => p.CompanyId == companyId && p.ProjectCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Project with code '{code}' already exists.");
        }

        var project = new Project
        {
            CompanyId = companyId,
            ProjectCode = code,
            ProjectName = request.ProjectName.Trim(),
            Location = request.Location.Trim(),
            TotalUnits = request.TotalUnits,
            EstimatedCost = request.EstimatedCost,
            Status = request.Status
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectDto(project.Id, project.CompanyId, project.ProjectCode, project.ProjectName, project.Location, project.TotalUnits, project.EstimatedCost, project.Status);
    }

    public async Task<IReadOnlyList<PropertyUnitDto>> GetPropertyUnitsAsync(int? projectId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from u in _dbContext.PropertyUnits.AsNoTracking()
                    where u.CompanyId == companyId
                    join p in _dbContext.Projects.AsNoTracking() on u.ProjectId equals p.Id
                    select new { u, ProjectName = p.ProjectName };

        if (projectId.HasValue)
        {
            query = query.Where(x => x.u.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.u.Status == status);
        }

        var list = await query.OrderBy(x => x.u.UnitNumber).ToListAsync(cancellationToken);

        return list.Select(x => new PropertyUnitDto(
            x.u.Id,
            x.u.CompanyId,
            x.u.ProjectId,
            x.ProjectName,
            x.u.UnitNumber,
            x.u.UnitType,
            x.u.FloorNumber,
            x.u.AreaSqFt,
            x.u.RatePerSqFt,
            x.u.TotalPrice,
            x.u.Status
        )).ToList();
    }

    public async Task<PropertyUnitDto> CreatePropertyUnitAsync(CreatePropertyUnitRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == request.ProjectId, cancellationToken);
        if (project == null)
        {
            throw new InvalidOperationException($"Project with ID {request.ProjectId} was not found.");
        }

        var unitNumber = request.UnitNumber.Trim().ToUpperInvariant();
        var exists = await _dbContext.PropertyUnits.AnyAsync(u => u.ProjectId == request.ProjectId && u.UnitNumber == unitNumber, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Unit '{unitNumber}' already exists in project '{project.ProjectName}'.");
        }

        var totalPrice = Math.Round(request.AreaSqFt * request.RatePerSqFt, 2);

        var unit = new PropertyUnit
        {
            CompanyId = companyId,
            ProjectId = request.ProjectId,
            UnitNumber = unitNumber,
            UnitType = request.UnitType.Trim(),
            FloorNumber = request.FloorNumber,
            AreaSqFt = request.AreaSqFt,
            RatePerSqFt = request.RatePerSqFt,
            TotalPrice = totalPrice,
            Status = "Available"
        };

        _dbContext.PropertyUnits.Add(unit);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyUnitDto(unit.Id, unit.CompanyId, unit.ProjectId, project.ProjectName, unit.UnitNumber, unit.UnitType, unit.FloorNumber, unit.AreaSqFt, unit.RatePerSqFt, unit.TotalPrice, unit.Status);
    }

    public async Task<IReadOnlyList<PropertyBookingDto>> GetPropertyBookingsAsync(int? projectId = null, int? customerId = null, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var query = from b in _dbContext.PropertyBookings.AsNoTracking()
                    where b.CompanyId == companyId
                    join p in _dbContext.Projects.AsNoTracking() on b.ProjectId equals p.Id
                    join u in _dbContext.PropertyUnits.AsNoTracking() on b.PropertyUnitId equals u.Id
                    join c in _dbContext.Customers.AsNoTracking() on b.CustomerId equals c.Id
                    select new { b, p.ProjectName, u.UnitNumber, c.CustomerName };

        if (projectId.HasValue)
        {
            query = query.Where(x => x.b.ProjectId == projectId.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(x => x.b.CustomerId == customerId.Value);
        }

        var list = await query.OrderByDescending(x => x.b.BookingDate).ToListAsync(cancellationToken);

        return list.Select(x => MapToBookingDto(x.b, x.ProjectName, x.UnitNumber, x.CustomerName, new List<BookingInstallmentDto>())).ToList();
    }

    public async Task<PropertyBookingDto?> GetPropertyBookingByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var booking = await _dbContext.PropertyBookings
            .Include(b => b.Project)
            .Include(b => b.PropertyUnit)
            .Include(b => b.Customer)
            .Include(b => b.Installments)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Id == id, cancellationToken);

        if (booking == null) return null;

        var installmentDtos = booking.Installments.OrderBy(i => i.InstallmentNumber).Select(i => new BookingInstallmentDto(
            i.Id,
            i.InstallmentNumber,
            i.DueDate,
            i.AmountDue,
            i.AmountPaid,
            i.Status,
            i.PaymentDate,
            i.JournalVoucherId,
            i.Remarks
        )).ToList();

        return MapToBookingDto(
            booking,
            booking.Project?.ProjectName ?? string.Empty,
            booking.PropertyUnit?.UnitNumber ?? string.Empty,
            booking.Customer?.CustomerName ?? string.Empty,
            installmentDtos
        );
    }

    public async Task<PropertyBookingDto> CreatePropertyBookingAsync(CreatePropertyBookingRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var unit = await _dbContext.PropertyUnits.FirstOrDefaultAsync(u => u.CompanyId == companyId && u.Id == request.PropertyUnitId, cancellationToken);
        if (unit == null)
        {
            throw new InvalidOperationException($"Property Unit with ID {request.PropertyUnitId} was not found.");
        }

        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Id == request.ProjectId, cancellationToken);
        if (project == null || unit.ProjectId != project.Id)
        {
            throw new InvalidOperationException($"Project with ID {request.ProjectId} was not found or does not match the property unit.");
        }

        if (unit.Status != "Available")
        {
            throw new InvalidOperationException($"Property Unit '{unit.UnitNumber}' is currently {unit.Status} and cannot be booked.");
        }

        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Id == request.CustomerId, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer with ID {request.CustomerId} was not found.");
        }

        var bookingNumber = $"BKG-{DateTime.UtcNow:yyyyMM}-{unit.UnitNumber}";
        var booking = new PropertyBooking
        {
            CompanyId = companyId,
            ProjectId = request.ProjectId,
            PropertyUnitId = request.PropertyUnitId,
            CustomerId = request.CustomerId,
            BookingNumber = bookingNumber,
            BookingDate = request.BookingDate.Date,
            AgreedPrice = request.AgreedPrice,
            DownPayment = request.DownPayment,
            TotalPaid = request.DownPayment,
            Status = "Active"
        };

        var installmentsStart = request.InstallmentsStartDate?.Date ?? request.BookingDate.Date;
        booking.GenerateEqualInstallments(request.NumberOfInstallments, installmentsStart);

        unit.Status = "Booked";

        // If Down payment was collected, record GL Journal Voucher:
        // Dr: 1120 Prime Bank Ltd. = DownPayment
        // Cr: 2150 Customer Booking Advances = DownPayment
        if (request.DownPayment > 0)
        {
            var bankAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1120", cancellationToken);
            var advanceAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2150", cancellationToken);

            if (bankAccount != null && advanceAccount != null)
            {
                var jvLines = new List<CreateVoucherLineRequest>
                {
                    new(
                        AccountId: bankAccount.Id,
                        Debit: request.DownPayment,
                        Credit: 0m,
                        Particulars: $"Down Payment for Booking {bookingNumber} ({unit.UnitNumber})"
                    ),
                    new(
                        AccountId: advanceAccount.Id,
                        Debit: 0m,
                        Credit: request.DownPayment,
                        Particulars: $"Customer Advance Liability: {customer.CustomerName} ({bookingNumber})"
                    )
                };

                var jvRequest = new CreateJournalVoucherRequest(
                    VoucherDate: DateOnly.FromDateTime(request.BookingDate),
                    Reference: bookingNumber,
                    Description: $"Property Booking Down Payment: {unit.UnitNumber} by {customer.CustomerName}",
                    FiscalPeriodId: null,
                    Lines: jvLines
                );

                var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
                if (createResult.IsSuccess)
                {
                    await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
                }
            }
        }

        _dbContext.PropertyBookings.Add(booking);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPropertyBookingByIdAsync(booking.Id, cancellationToken))!;
    }

    public async Task<InstallmentCollectionResultDto> CollectInstallmentAsync(CollectInstallmentRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = GetCompanyId();
        var booking = await _dbContext.PropertyBookings
            .Include(b => b.Customer)
            .Include(b => b.PropertyUnit)
            .Include(b => b.Installments)
            .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            throw new InvalidOperationException($"Booking with ID {request.BookingId} was not found.");
        }

        var installment = booking.Installments.FirstOrDefault(i => i.Id == request.InstallmentId);
        if (installment == null)
        {
            throw new InvalidOperationException($"Installment with ID {request.InstallmentId} was not found in booking {booking.BookingNumber}.");
        }

        if (request.AmountPaid <= 0)
        {
            throw new InvalidOperationException("Installment payment amount must be greater than zero.");
        }

        installment.AmountPaid += request.AmountPaid;
        installment.PaymentDate = request.PaymentDate.Date;
        if (installment.AmountPaid >= installment.AmountDue)
        {
            installment.Status = "Paid";
        }
        else
        {
            installment.Status = "PartiallyPaid";
        }

        booking.TotalPaid += request.AmountPaid;

        // Automated Double-Entry GL Journal Voucher:
        // Dr: 1120 Prime Bank Ltd. (or selected deposit account)
        // Cr: 2150 Customer Booking Advances & Deposits
        var bankAccount = request.DepositBankAccountId.HasValue
            ? await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Id == request.DepositBankAccountId.Value, cancellationToken)
            : await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "1120", cancellationToken);

        var advanceAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "2150", cancellationToken);

        if (bankAccount == null || advanceAccount == null)
        {
            throw new InvalidOperationException("Bank Account (1120) or Customer Advance Account (2150) was not found in Chart of Accounts.");
        }

        var jvLines = new List<CreateVoucherLineRequest>
        {
            new(
                AccountId: bankAccount.Id,
                Debit: request.AmountPaid,
                Credit: 0m,
                Particulars: $"Installment {installment.InstallmentNumber} collection for {booking.BookingNumber}"
            ),
            new(
                AccountId: advanceAccount.Id,
                Debit: 0m,
                Credit: request.AmountPaid,
                Particulars: $"Customer Booking Advance: {booking.Customer?.CustomerName} ({booking.PropertyUnit?.UnitNumber})"
            )
        };

        var refCode = request.ReferenceNumber ?? $"INS-{booking.BookingNumber}-{installment.InstallmentNumber}";
        var jvRequest = new CreateJournalVoucherRequest(
            VoucherDate: DateOnly.FromDateTime(request.PaymentDate),
            Reference: refCode,
            Description: $"Real Estate Installment #{installment.InstallmentNumber} Collection: {booking.BookingNumber} from {booking.Customer?.CustomerName}",
            FiscalPeriodId: null,
            Lines: jvLines
        );

        var createResult = await _journalVoucherService.CreateVoucherAsync(jvRequest, cancellationToken);
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create GL voucher for installment collection: {createResult.Error}");
        }

        var postResult = await _journalVoucherService.PostVoucherAsync(createResult.Value!.Id, cancellationToken);
        if (!postResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to post GL voucher for installment collection: {postResult.Error}");
        }

        installment.JournalVoucherId = createResult.Value!.Id;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var remainingBalance = Math.Max(0m, booking.AgreedPrice - booking.TotalPaid);

        return new InstallmentCollectionResultDto(
            BookingId: booking.Id,
            BookingNumber: booking.BookingNumber,
            InstallmentId: installment.Id,
            InstallmentNumber: installment.InstallmentNumber,
            AmountPaid: request.AmountPaid,
            RemainingBookingBalance: remainingBalance,
            JournalVoucherId: createResult.Value!.Id,
            JournalVoucherNumber: createResult.Value!.VoucherNumber
        );
    }

    private static PropertyBookingDto MapToBookingDto(PropertyBooking b, string projName, string unitNum, string custName, IReadOnlyList<BookingInstallmentDto> installments)
    {
        var balanceDue = Math.Max(0m, b.AgreedPrice - b.TotalPaid);
        return new PropertyBookingDto(
            b.Id,
            b.CompanyId,
            b.ProjectId,
            projName,
            b.PropertyUnitId,
            unitNum,
            b.CustomerId,
            custName,
            b.BookingNumber,
            b.BookingDate,
            b.AgreedPrice,
            b.DownPayment,
            b.TotalPaid,
            balanceDue,
            b.Status,
            installments
        );
    }
}
