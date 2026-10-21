namespace ERP.Common.Application.Abstractions.Identity;

public interface ITokenRevocationService
{
    void RevokeToken(string token);
    bool IsTokenRevoked(string token);
}
