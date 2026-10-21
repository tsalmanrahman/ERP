using System.Collections.Concurrent;
using ERP.Common.Application.Abstractions.Identity;

namespace ERP.Common.Infrastructure.Services.Identity;

public class TokenRevocationService : ITokenRevocationService
{
    private readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

    public void RevokeToken(string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _revokedTokens.TryAdd(token.Trim(), DateTime.UtcNow);
        }
    }

    public bool IsTokenRevoked(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return _revokedTokens.ContainsKey(token.Trim());
    }
}
