namespace ERP.Common.Application.Security;

/// <summary>
/// Strongly-typed JWT configuration bound from the "Jwt" configuration section.
/// The signing key must be supplied through configuration (user-secrets, environment variable
/// <c>Jwt__SecretKey</c>, or a secret store); there is intentionally no built-in fallback key.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum signing-key length (HMAC-SHA256 requires at least 256 bits).</summary>
    public const int MinimumSecretKeyLength = 32;

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "ERP.Security";
    public string Audience { get; set; } = "ERP.Client";
    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Throws when the configuration is unsafe to run with (missing or too-short signing key).
    /// </summary>
    public JwtSettings EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(SecretKey) || SecretKey.Length < MinimumSecretKeyLength)
        {
            throw new InvalidOperationException(
                $"Configuration '{SectionName}:SecretKey' is missing or shorter than {MinimumSecretKeyLength} characters. " +
                "Provide it via user-secrets (development) or the 'Jwt__SecretKey' environment variable (deployment).");
        }

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException($"Configuration '{SectionName}:Issuer' and '{SectionName}:Audience' are required.");
        }

        if (ExpiryMinutes <= 0)
        {
            throw new InvalidOperationException($"Configuration '{SectionName}:ExpiryMinutes' must be greater than zero.");
        }

        return this;
    }
}
