namespace ERP.Common.Application.Models.Finance;

public record AccountDto(
    int Id,
    string AccountCode,
    string AccountName,
    int AccountGroupId,
    string GroupName,
    string Category,
    int AccountType,
    bool IsReconcilable,
    bool IsActive
);

public record AccountGroupDto(
    int Id,
    string GroupCode,
    string GroupName,
    string Category
);

public class CreateAccountRequest
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int AccountGroupId { get; set; } = 1;
    public int AccountType { get; set; } = 2; // 1=Header, 2=Posting
    public bool IsReconcilable { get; set; }
    public bool IsActive { get; set; } = true;
}
