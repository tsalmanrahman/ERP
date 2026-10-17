using ERP.Common.Application.Abstractions.Common;

namespace ERP.Common.Infrastructure.Services.Common;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
