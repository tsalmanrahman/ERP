using InteAcc.Common.Domain.Common;
using InteAcc.Common.Domain.Entities.AccountsReceivable;

namespace InteAcc.Common.Domain.Entities.RealEstate;

public class PropertyBooking : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int PropertyUnitId { get; set; }
    public PropertyUnit? PropertyUnit { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string BookingNumber { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public decimal AgreedPrice { get; set; }
    public decimal DownPayment { get; set; }
    public decimal TotalPaid { get; set; }
    public string Status { get; set; } = "Active"; // Active, Cancelled, Completed

    // Auditing & Concurrency
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<BookingInstallment> Installments { get; set; } = new List<BookingInstallment>();

    public PropertyBooking() { }

    public void GenerateEqualInstallments(int numberOfInstallments, DateTime startDate)
    {
        Installments.Clear();
        var remainingBalance = AgreedPrice - DownPayment;
        if (numberOfInstallments <= 0 || remainingBalance <= 0) return;

        var installmentAmount = Math.Round(remainingBalance / numberOfInstallments, 2);
        decimal runningTotal = 0;

        for (int i = 1; i <= numberOfInstallments; i++)
        {
            decimal currentAmt = (i == numberOfInstallments) ? (remainingBalance - runningTotal) : installmentAmount;
            runningTotal += currentAmt;

            Installments.Add(new BookingInstallment
            {
                InstallmentNumber = i,
                DueDate = startDate.AddMonths(i),
                AmountDue = currentAmt,
                AmountPaid = 0m,
                Status = "Pending",
                Remarks = $"Installment {i} of {numberOfInstallments}"
            });
        }
    }
}

public class BookingInstallment : Entity<int>
{
    public int BookingId { get; set; }
    public PropertyBooking? Booking { get; set; }

    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Paid, Overdue
    public DateTime? PaymentDate { get; set; }
    public int? JournalVoucherId { get; set; }
    public string? Remarks { get; set; }

    public BookingInstallment() { }
}
