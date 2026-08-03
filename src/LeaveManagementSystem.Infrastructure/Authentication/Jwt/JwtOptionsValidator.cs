using System.Text;
using Microsoft.Extensions.Options;

namespace LeaveManagementSystem.Infrastructure.Authentication.Jwt;

public sealed class JwtOptionsValidator
    : IValidateOptions<JwtOptions>
{
    private const int MinimumSigningKeySizeInBytes = 32;

    private const int MaximumAccessTokenExpirationMinutes =
        1440;

    public ValidateOptionsResult Validate(
        string? name,
        JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add(
                "Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add(
                "Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            failures.Add(
                "Jwt:SigningKey is required.");
        }
        else if (Encoding.UTF8.GetByteCount(options.SigningKey)
                 < MinimumSigningKeySizeInBytes)
        {
            failures.Add(
                "Jwt:SigningKey must contain at least 32 bytes.");
        }

        if (options.AccessTokenExpirationMinutes is < 1
            or > MaximumAccessTokenExpirationMinutes)
        {
            failures.Add(
                "Jwt:AccessTokenExpirationMinutes must be between 1 and 1440.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
