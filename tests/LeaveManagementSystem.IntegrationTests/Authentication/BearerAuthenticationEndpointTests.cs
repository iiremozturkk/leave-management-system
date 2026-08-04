using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Constants;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.Infrastructure.Authentication.Jwt;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using LeaveManagementSystem.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.Authentication;

public sealed class BearerAuthenticationEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private static readonly string[] RequiredClaimTypeValues =
{
    JwtRegisteredClaimNames.Sub,
    JwtRegisteredClaimNames.Jti,
    JwtRegisteredClaimNames.Email,
    JwtClaimNames.EmployeeId,
    JwtClaimNames.Role
};

    public static IEnumerable<object[]> RequiredClaimTypes
    {
        get
        {
            foreach (var claimType in RequiredClaimTypeValues)
            {
                yield return
                    new object[]
                    {
                    claimType
                    };
            }
        }
    }

    public static IEnumerable<object[]> InvalidRequiredClaimValues
    {
        get
        {
            foreach (var claimType in RequiredClaimTypeValues)
            {
                yield return
                    new object[]
                    {
                    claimType,
                    string.Empty
                    };

                yield return
                    new object[]
                    {
                    claimType,
                    "   "
                    };
            }
        }
    }

    public static IEnumerable<object[]> InvalidGuidClaims
    {
        get
        {
            var claimTypes =
                new[]
                {
                JwtRegisteredClaimNames.Sub,
                JwtRegisteredClaimNames.Jti,
                JwtClaimNames.EmployeeId
                };

            var invalidValues =
                new[]
                {
                "not-a-guid",
                Guid.Empty.ToString("D")
                };

            foreach (var claimType in claimTypes)
            {
                foreach (var invalidValue in invalidValues)
                {
                    yield return
                        new object[]
                        {
                        claimType,
                        invalidValue
                        };
                }
            }
        }
    }

    [Fact]
    public async Task GetClaims_WithoutToken_ReturnsUnauthorized()
    {
        using var response =
            await _client.GetAsync(
                "/api/test-authentication/claims");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Contains(
            response.Headers.WwwAuthenticate,
            header =>
                string.Equals(
                    header.Scheme,
                    "Bearer",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetClaims_WithMalformedToken_ReturnsUnauthorized()
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/test-authentication/claims");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                "this-is-not-a-valid-jwt");

        using var response =
            await _client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Contains(
            response.Headers.WwwAuthenticate,
            header =>
                string.Equals(
                    header.Scheme,
                    "Bearer",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(RequiredClaimTypes))]
    public async Task GetClaims_WithMissingRequiredClaim_ReturnsUnauthorized(
        string claimType)
    {
        var claims =
            CreateValidClaims();

        claims.RemoveAll(
            claim =>
                claim.Type == claimType);

        var token =
            CreateTestToken(
                claims: claims);

        await AssertTokenRejectedAsync(
            token);
    }

    [Theory]
    [MemberData(nameof(RequiredClaimTypes))]
    public async Task GetClaims_WithDuplicateRequiredClaim_ReturnsUnauthorized(
        string claimType)
    {
        var claims =
            CreateValidClaims();

        var existingClaim =
            Assert.Single(
                claims,
                claim =>
                    claim.Type == claimType);

        claims.Add(
            new Claim(
                existingClaim.Type,
                existingClaim.Value));

        var token =
            CreateTestToken(
                claims: claims);

        await AssertTokenRejectedAsync(
            token);
    }

    [Theory]
    [MemberData(nameof(InvalidRequiredClaimValues))]
    public async Task GetClaims_WithEmptyOrWhitespaceRequiredClaim_ReturnsUnauthorized(
    string claimType,
    string claimValue)
    {
        var claims =
            CreateValidClaims();

        ReplaceClaim(
            claims,
            claimType,
            claimValue);

        var token =
            CreateTestToken(
                claims: claims);

        await AssertTokenRejectedAsync(
            token);
    }

    [Theory]
    [MemberData(nameof(InvalidGuidClaims))]
    public async Task GetClaims_WithInvalidGuidRequiredClaim_ReturnsUnauthorized(
        string claimType,
        string claimValue)
    {
        var claims =
            CreateValidClaims();

        ReplaceClaim(
            claims,
            claimType,
            claimValue);

        var token =
            CreateTestToken(
                claims: claims);

        await AssertTokenRejectedAsync(
            token);
    }

    [Theory]
    [InlineData("manager")]
    [InlineData("3")]
    [InlineData("NotARole")]
    public async Task GetClaims_WithInvalidRoleRequiredClaim_ReturnsUnauthorized(
        string claimValue)
    {
        var claims =
            CreateValidClaims();

        ReplaceClaim(
            claims,
            JwtClaimNames.Role,
            claimValue);

        var token =
            CreateTestToken(
                claims: claims);

        await AssertTokenRejectedAsync(
            token);
    }

    [Fact]
    public async Task GetClaims_WithValidLoginToken_ReturnsAuthenticatedClaims()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            const string password =
                "Bearer-Integration-Test-Password-123!";

            var testUser =
                await CreateUserAccountAsync(
                    departmentId,
                    password);

            employeeId = testUser.EmployeeId;

            var loginRequest = new
            {
                email = testUser.Email,
                password
            };

            using var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    loginRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                loginResponse.StatusCode);

            var loginResult =
                await loginResponse.Content
                    .ReadFromJsonAsync<LoginResponse>(
                        JsonOptions);

            Assert.NotNull(
                loginResult);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    loginResult!.AccessToken));

            using var claimsRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "/api/test-authentication/claims");

            claimsRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    loginResult.AccessToken);

            using var claimsResponse =
                await _client.SendAsync(
                    claimsRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                claimsResponse.StatusCode);

            var claims =
                await claimsResponse.Content
                    .ReadFromJsonAsync<AuthenticationClaimsResponse>(
                        JsonOptions);

            Assert.NotNull(
                claims);

            Assert.True(
                claims!.IsAuthenticated);

            Assert.Equal(
                testUser.UserAccountId.ToString("D"),
                claims.UserAccountId);

            Assert.Equal(
                testUser.EmployeeId.ToString("D"),
                claims.EmployeeId);

            Assert.Equal(
                testUser.Email,
                claims.Email);

            Assert.Equal(
                EmployeeRole.Employee.ToString(),
                claims.Role);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId,
                departmentId);
        }
    }

    [Fact]
    public async Task GetClaims_WithExpiredToken_ReturnsUnauthorized()
    {
        var nowUtc =
            DateTime.UtcNow;

        var token =
            CreateTestToken(
                issuedAtUtc: nowUtc.AddMinutes(-10),
                notBeforeUtc: nowUtc.AddMinutes(-10),
                expiresAtUtc: nowUtc.AddMinutes(-5));

        await AssertTokenRejectedAsync(
            token);
    }

    [Fact]
    public async Task GetClaims_WithInvalidSigningKey_ReturnsUnauthorized()
    {
        var token =
            CreateTestToken(
                signingKey:
                    "invalid-test-signing-key-that-is-long-enough-for-hmac-sha256");

        await AssertTokenRejectedAsync(
            token);
    }

    [Fact]
    public async Task GetClaims_WithInvalidIssuer_ReturnsUnauthorized()
    {
        var token =
            CreateTestToken(
                issuer:
                    "Invalid.IntegrationTests.Issuer");

        await AssertTokenRejectedAsync(
            token);
    }

    [Fact]
    public async Task GetClaims_WithInvalidAudience_ReturnsUnauthorized()
    {
        var token =
            CreateTestToken(
                audience:
                    "Invalid.IntegrationTests.Audience");

        await AssertTokenRejectedAsync(
            token);
    }

    [Fact]
    public async Task GetClaims_WithUnsupportedAlgorithm_ReturnsUnauthorized()
    {
        var token =
            CreateTestToken(
                algorithm:
                    SecurityAlgorithms.HmacSha512);

        await AssertTokenRejectedAsync(
            token);
    }

    private string CreateTestToken(
        string? issuer = null,
        string? audience = null,
        string? signingKey = null,
        string algorithm = SecurityAlgorithms.HmacSha256,
        DateTime? issuedAtUtc = null,
        DateTime? notBeforeUtc = null,
        DateTime? expiresAtUtc = null,
        IEnumerable<Claim>? claims = null)
    {
        using var scope =
            _factory.Services.CreateScope();

        var jwtOptions =
            scope.ServiceProvider
                .GetRequiredService<IOptions<JwtOptions>>()
                .Value;

        var nowUtc =
            DateTime.UtcNow;

        var actualIssuedAtUtc =
            issuedAtUtc ?? nowUtc;

        var actualNotBeforeUtc =
            notBeforeUtc ?? nowUtc.AddMinutes(-1);

        var actualExpiresAtUtc =
            expiresAtUtc ?? nowUtc.AddMinutes(5);

        var actualClaims =
            claims?.ToArray()
            ?? CreateValidClaims().ToArray();

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    signingKey ??
                    jwtOptions.SigningKey));

        var tokenDescriptor =
            new SecurityTokenDescriptor
            {
                Issuer =
                    issuer ??
                    jwtOptions.Issuer,

                Audience =
                    audience ??
                    jwtOptions.Audience,

                Subject =
                    new ClaimsIdentity(
                        actualClaims),

                IssuedAt =
                    actualIssuedAtUtc,

                NotBefore =
                    actualNotBeforeUtc,

                Expires =
                    actualExpiresAtUtc,

                SigningCredentials =
                    new SigningCredentials(
                        securityKey,
                        algorithm)
            };

        var tokenHandler =
            new JwtSecurityTokenHandler();

        var securityToken =
            tokenHandler.CreateToken(
                tokenDescriptor);

        return tokenHandler.WriteToken(
            securityToken);
    }

    private static List<Claim> CreateValidClaims()
    {
        return
            new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    Guid.NewGuid().ToString("D")),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString("D")),

                new(
                    JwtRegisteredClaimNames.Email,
                    "jwt.validation@example.com"),

                new(
                    JwtClaimNames.EmployeeId,
                    Guid.NewGuid().ToString("D")),

                new(
                    JwtClaimNames.Role,
                    EmployeeRole.Employee.ToString())
            };
    }

    private static void ReplaceClaim(
        List<Claim> claims,
        string claimType,
        string replacementValue)
    {
        claims.RemoveAll(
            claim =>
                claim.Type == claimType);

        claims.Add(
            new Claim(
                claimType,
                replacementValue));
    }

    private async Task AssertTokenRejectedAsync(
        string token)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/test-authentication/claims");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        using var response =
            await _client.SendAsync(
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Contains(
            response.Headers.WwwAuthenticate,
            header =>
                string.Equals(
                    header.Scheme,
                    "Bearer",
                    StringComparison.OrdinalIgnoreCase));

        var authenticateHeader =
            string.Join(
                ", ",
                response.Headers.WwwAuthenticate
                    .Select(header =>
                        header.ToString()));

        Assert.False(
            authenticateHeader.Contains(
                "error_description",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            authenticateHeader.Contains(
                "The access token is invalid.",
                StringComparison.OrdinalIgnoreCase));
    }

    private async Task<TestUserData> CreateUserAccountAsync(
        Guid departmentId,
        string password)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var passwordHashService =
            scope.ServiceProvider
                .GetRequiredService<IPasswordHashService>();

        var employeeId =
            Guid.NewGuid();

        var email =
            $"integration.bearer.{Guid.NewGuid():N}@example.com";

        var employee =
            new Employee
            {
                Id = employeeId,
                FirstName = "Integration",
                LastName = "BearerUser",
                Email = email,
                DepartmentId = departmentId,
                ManagerId = null,
                Role = EmployeeRole.Employee,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = null
            };

        var passwordHash =
            passwordHashService.HashPassword(
                password);

        var userAccount =
            new UserAccount(
                employeeId,
                passwordHash);

        dbContext.Employees.Add(
            employee);

        dbContext.UserAccounts.Add(
            userAccount);

        await dbContext.SaveChangesAsync();

        return new TestUserData(
            userAccount.Id,
            employeeId,
            email);
    }

    private sealed record TestUserData(
        Guid UserAccountId,
        Guid EmployeeId,
        string Email);
}
