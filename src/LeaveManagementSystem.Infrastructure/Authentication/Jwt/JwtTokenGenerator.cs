using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Constants;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LeaveManagementSystem.Infrastructure.Authentication.Jwt;

public sealed class JwtTokenGenerator
    : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public JwtTokenGenerator(
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(jwtOptions);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public JwtTokenResult GenerateToken(
        JwtTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var issuedAtUtc =
            _timeProvider.GetUtcNow().UtcDateTime;

        var expiresAtUtc =
            issuedAtUtc.AddMinutes(
                _jwtOptions.AccessTokenExpirationMinutes);

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    request.UserAccountId.ToString("D")),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString("D")),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    request.Email),

                new Claim(
                    JwtClaimNames.EmployeeId,
                    request.EmployeeId.ToString("D")),

                new Claim(
                    JwtClaimNames.Role,
                    request.Role.ToString())
            };

        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _jwtOptions.SigningKey));

        var signingCredentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var tokenDescriptor =
            new SecurityTokenDescriptor
            {
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                Subject = new ClaimsIdentity(claims),
                IssuedAt = issuedAtUtc,
                NotBefore = issuedAtUtc,
                Expires = expiresAtUtc,
                SigningCredentials = signingCredentials
            };

        var tokenHandler =
            new JwtSecurityTokenHandler();

        var securityToken =
            tokenHandler.CreateToken(
                tokenDescriptor);

        var accessToken =
            tokenHandler.WriteToken(
                securityToken);

        return new JwtTokenResult(
            accessToken,
            expiresAtUtc);
    }

    private static void ValidateRequest(
        JwtTokenRequest request)
    {
        if (request.UserAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "User account id cannot be empty.",
                nameof(request.UserAccountId));
        }

        if (request.EmployeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Employee id cannot be empty.",
                nameof(request.EmployeeId));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException(
                "Email cannot be empty.",
                nameof(request.Email));
        }

        if (!Enum.IsDefined(
                typeof(EmployeeRole),
                request.Role))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Role),
                request.Role,
                "Employee role is not supported.");
        }
    }
}
