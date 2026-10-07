using InteAcc.Common.Application.Abstractions.Common;

namespace InteAcc.Common.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
