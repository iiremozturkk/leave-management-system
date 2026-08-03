using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Commands.Login;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Domain.Entities;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.Authentication.Login;

public sealed class LoginCommandHandlerTests
{
    private const string NormalizedEmail = "employee@example.com";
    private const string Password = " Correct-Horse-Battery-Staple-123! ";
    private const string StoredPasswordHash = "stored-password-hash";
    private const string NewPasswordHash = "new-password-hash";

    private static readonly DateTime TokenExpiresAtUtc =
        new(2026, 8, 3, 13, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_NullCommand_ThrowsBeforeDependencyCalls()
    {
        var context = new TestContext();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.Handler.Handle(null!, CancellationToken.None));

        Assert.Equal("request", exception.ParamName);
        Assert.Equal(0, context.ReadRepository.GetByEmailCallCount);
        Assert.Equal(0, context.PasswordHashService.VerifyPasswordCallCount);
        Assert.Equal(0, context.WriteRepository.GetForUpdateCallCount);
        Assert.Equal(0, context.PasswordHashService.HashPasswordCallCount);
        Assert.Equal(0, context.WriteRepository.SaveChangesCallCount);
        Assert.Equal(0, context.JwtTokenGenerator.GenerateTokenCallCount);
        Assert.Empty(context.CallSequence);
    }

    [Fact]
    public async Task Handle_EmailWithWhitespaceAndUppercase_NormalizesEmailBeforeLookup()
    {
        var context = new TestContext();
        context.ReadRepository.AllowGetByEmail = true;

        var result = await context.Handler.Handle(
            new LoginCommand("  Employee@Example.COM  ", Password),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(
            NormalizedEmail,
            Assert.Single(context.ReadRepository.RequestedNormalizedEmails));
        Assert.Equal(new[] { "GetByEmail" }, context.CallSequence);
    }

    [Fact]
    public async Task Handle_UserAccountDoesNotExist_ReturnsNullAndStopsProcessing()
    {
        var context = new TestContext();
        context.ReadRepository.AllowGetByEmail = true;

        var result = await context.Handler.Handle(
            CreateValidCommand(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, context.PasswordHashService.VerifyPasswordCallCount);
        Assert.Equal(0, context.WriteRepository.GetForUpdateCallCount);
        Assert.Equal(0, context.JwtTokenGenerator.GenerateTokenCallCount);
        Assert.Equal(new[] { "GetByEmail" }, context.CallSequence);
    }

    [Fact]
    public async Task Handle_IncorrectPassword_ReturnsNullAndStopsProcessing()
    {
        var context = CreateContextWithAuthenticationData();
        context.PasswordHashService.AllowVerifyPassword = true;
        context.PasswordHashService.VerificationOutcome =
            PasswordVerificationOutcome.Failed;

        var result = await context.Handler.Handle(
            CreateValidCommand(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(
            StoredPasswordHash,
            Assert.Single(context.PasswordHashService.VerifiedPasswordHashes));
        Assert.Equal(
            Password,
            Assert.Single(context.PasswordHashService.VerifiedProvidedPasswords));
        Assert.Equal(0, context.WriteRepository.GetForUpdateCallCount);
        Assert.Equal(0, context.JwtTokenGenerator.GenerateTokenCallCount);
        Assert.Equal(
            new[] { "GetByEmail", "VerifyPassword" },
            context.CallSequence);
    }

    [Fact]
    public async Task Handle_InactiveUserAccount_ReturnsNullAndDoesNotGenerateToken()
    {
        var context = CreateContextWithAuthenticationData(
            isUserAccountActive: false);

        context.PasswordHashService.AllowVerifyPassword = true;
        context.PasswordHashService.VerificationOutcome =
            PasswordVerificationOutcome.Succeeded;

        var result = await context.Handler.Handle(
            CreateValidCommand(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, context.WriteRepository.GetForUpdateCallCount);
        Assert.Equal(0, context.JwtTokenGenerator.GenerateTokenCallCount);
        Assert.Equal(
            new[] { "GetByEmail", "VerifyPassword" },
            context.CallSequence);
    }

    [Fact]
    public async Task Handle_InactiveEmployee_ReturnsNullAndDoesNotGenerateToken()
    {
        var context = CreateContextWithAuthenticationData(
            isEmployeeActive: false);

        context.PasswordHashService.AllowVerifyPassword = true;
        context.PasswordHashService.VerificationOutcome =
            PasswordVerificationOutcome.Succeeded;

        var result = await context.Handler.Handle(
            CreateValidCommand(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, context.WriteRepository.GetForUpdateCallCount);
        Assert.Equal(0, context.JwtTokenGenerator.GenerateTokenCallCount);
        Assert.Equal(
            new[] { "GetByEmail", "VerifyPassword" },
            context.CallSequence);
    }

    [Fact]
    public async Task Handle_UnsupportedVerificationOutcome_ThrowsAndDoesNotGenerateToken()
    {
        var context = CreateContextWithAuthenticationData();
        context.PasswordHashService.AllowVerifyPassword = true;
        context.PasswordHashService.VerificationOutcome =
            (PasswordVerificationOutcome)999;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Handler.Handle(
                CreateValidCommand(),
                CancellationToken.None));

        Assert.Equal(
            "Unsupported password verification outcome.",
            exception.Message);

        Assert.Equal(0, context.WriteRepository.GetForUpdateCallCount);
        Assert.Equal(0, context.JwtTokenGenerator.GenerateTokenCallCount);
        Assert.Equal(
            new[] { "GetByEmail", "VerifyPassword" },
            context.CallSequence);
    }

    [Fact]
    public async Task Handle_ValidCredentials_GeneratesTokenAndReturnsLoginResult()
    {
        var authenticationData = CreateAuthenticationData();
        var context = CreateContextWithAuthenticationData(authenticationData);

        var tokenResult = new JwtTokenResult(
            "generated-access-token",
            TokenExpiresAtUtc);

        context.PasswordHashService.AllowVerifyPassword = true;
        context.PasswordHashService.VerificationOutcome =
            PasswordVerificationOutcome.Succeeded;

        context.JwtTokenGenerator.AllowGenerateToken = true;
        context.JwtTokenGenerator.Result = tokenResult;

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var result = await context.Handler.Handle(
            new LoginCommand("  Employee@Example.COM  ", Password),
            cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(tokenResult.AccessToken, result.AccessToken);
        Assert.Equal(tokenResult.ExpiresAtUtc, result.ExpiresAtUtc);
        Assert.Equal(authenticationData.UserAccountId, result.UserAccountId);
        Assert.Equal(authenticationData.EmployeeId, result.EmployeeId);
        Assert.Equal(authenticationData.Email, result.Email);
        Assert.Equal(authenticationData.Role, result.Role);

        Assert.Equal(
            NormalizedEmail,
            Assert.Single(context.ReadRepository.RequestedNormalizedEmails));

        Assert.Equal(
            cancellationToken,
            Assert.Single(context.ReadRepository.ReceivedCancellationTokens));

        Assert.Equal(
            Password,
            Assert.Single(context.PasswordHashService.VerifiedProvidedPasswords));

        Assert.Equal(0, context.PasswordHashService.HashPasswordCallCount);
        Assert.Equal(0, context.WriteRepository.GetForUpdateCallCount);
        Assert.Equal(0, context.WriteRepository.SaveChangesCallCount);

        var tokenRequest =
            Assert.Single(context.JwtTokenGenerator.RequestedTokenRequests);

        Assert.Equal(authenticationData.UserAccountId, tokenRequest.UserAccountId);
        Assert.Equal(authenticationData.EmployeeId, tokenRequest.EmployeeId);
        Assert.Equal(authenticationData.Email, tokenRequest.Email);
        Assert.Equal(authenticationData.Role, tokenRequest.Role);

        Assert.Equal(
            new[] { "GetByEmail", "VerifyPassword", "GenerateToken" },
            context.CallSequence);
    }

    [Fact]
    public async Task Handle_RehashNeeded_UpdatesHashSavesAndGeneratesToken()
    {
        var authenticationData = CreateAuthenticationData();
        var userAccount = CreateUserAccount(authenticationData);
        var context = CreateContextWithAuthenticationData(authenticationData);

        context.WriteRepository.AllowGetForUpdate = true;
        context.WriteRepository.AllowSaveChanges = true;
        context.WriteRepository.Result = userAccount;

        context.PasswordHashService.AllowVerifyPassword = true;
        context.PasswordHashService.AllowHashPassword = true;
        context.PasswordHashService.VerificationOutcome =
            PasswordVerificationOutcome.SucceededRehashNeeded;
        context.PasswordHashService.HashResult = NewPasswordHash;

        context.JwtTokenGenerator.AllowGenerateToken = true;
        context.JwtTokenGenerator.Result = new JwtTokenResult(
            "rehash-access-token",
            TokenExpiresAtUtc);

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var beforeRehashUtc = DateTime.UtcNow;

        var result = await context.Handler.Handle(
            CreateValidCommand(),
            cancellationToken);

        var afterRehashUtc = DateTime.UtcNow;

        Assert.NotNull(result);
        Assert.Equal("rehash-access-token", result.AccessToken);
        Assert.Equal(NewPasswordHash, userAccount.PasswordHash);
        Assert.NotNull(userAccount.UpdatedAtUtc);
        Assert.InRange(
            userAccount.UpdatedAtUtc.Value,
            beforeRehashUtc,
            afterRehashUtc);

        Assert.Equal(
            authenticationData.UserAccountId,
            Assert.Single(context.WriteRepository.RequestedUserAccountIds));

        Assert.Equal(
            cancellationToken,
            Assert.Single(context.WriteRepository.GetForUpdateTokens));

        Assert.Equal(
            cancellationToken,
            Assert.Single(context.WriteRepository.SaveChangesTokens));

        Assert.Equal(
            Password,
            Assert.Single(context.PasswordHashService.HashedPasswords));

        Assert.Equal(1, context.WriteRepository.SaveChangesCallCount);
        Assert.Single(context.JwtTokenGenerator.RequestedTokenRequests);

        Assert.Equal(
            new[]
            {
                "GetByEmail",
                "VerifyPassword",
                "GetForUpdate",
                "HashPassword",
                "SaveChanges",
                "GenerateToken"
            },
            context.CallSequence);
    }

    [Fact]
    public async Task Handle_RehashNeededAndUserAccountDoesNotExist_ReturnsNull()
    {
        var context = CreateContextWithAuthenticationData();

        context.WriteRepository.AllowGetForUpdate = true;
        context.PasswordHashService.AllowVerifyPassword = true;
        context.PasswordHashService.VerificationOutcome =
            PasswordVerificationOutcome.SucceededRehashNeeded;

        var result = await context.Handler.Handle(
            CreateValidCommand(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, context.PasswordHashService.HashPasswordCallCount);
        Assert.Equal(0, context.WriteRepository.SaveChangesCallCount);
        Assert.Equal(0, context.JwtTokenGenerator.GenerateTokenCallCount);
        Assert.Equal(
            new[] { "GetByEmail", "VerifyPassword", "GetForUpdate" },
            context.CallSequence);
    }

    [Fact]
    public async Task Handle_RehashNeededAndUserAccountIsInactive_ReturnsNull()
    {
        var authenticationData = CreateAuthenticationData();
        var userAccount = CreateUserAccount(authenticationData);
        userAccount.Deactivate();

        var context = CreateContextWithAuthenticationData(authenticationData);
        context.WriteRepository.AllowGetForUpdate = true;
        context.WriteRepository.Result = userAccount;

        context.PasswordHashService.AllowVerifyPassword = true;
        context.PasswordHashService.VerificationOutcome =
            PasswordVerificationOutcome.SucceededRehashNeeded;

        var result = await context.Handler.Handle(
            CreateValidCommand(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(StoredPasswordHash, userAccount.PasswordHash);
        Assert.Equal(0, context.PasswordHashService.HashPasswordCallCount);
        Assert.Equal(0, context.WriteRepository.SaveChangesCallCount);
        Assert.Equal(0, context.JwtTokenGenerator.GenerateTokenCallCount);
        Assert.Equal(
            new[] { "GetByEmail", "VerifyPassword", "GetForUpdate" },
            context.CallSequence);
    }

    [Fact]
    public async Task Handle_RehashNeededAndPasswordHashChanged_ReturnsNullWithoutOverwritingNewHash()
    {
        const string concurrentlyChangedHash =
            "concurrently-changed-password-hash";

        var authenticationData = CreateAuthenticationData();
        var userAccount = CreateUserAccount(
            authenticationData,
            concurrentlyChangedHash);

        var context = CreateContextWithAuthenticationData(authenticationData);
        context.WriteRepository.AllowGetForUpdate = true;
        context.WriteRepository.Result = userAccount;

        context.PasswordHashService.AllowVerifyPassword = true;
        context.PasswordHashService.VerificationOutcome =
            PasswordVerificationOutcome.SucceededRehashNeeded;

        var result = await context.Handler.Handle(
            CreateValidCommand(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(concurrentlyChangedHash, userAccount.PasswordHash);
        Assert.Equal(0, context.PasswordHashService.HashPasswordCallCount);
        Assert.Equal(0, context.WriteRepository.SaveChangesCallCount);
        Assert.Equal(0, context.JwtTokenGenerator.GenerateTokenCallCount);
        Assert.Equal(
            new[] { "GetByEmail", "VerifyPassword", "GetForUpdate" },
            context.CallSequence);
    }

    private static TestContext CreateContextWithAuthenticationData(
        bool isUserAccountActive = true,
        bool isEmployeeActive = true)
    {
        return CreateContextWithAuthenticationData(
            CreateAuthenticationData(
                isUserAccountActive,
                isEmployeeActive));
    }

    private static TestContext CreateContextWithAuthenticationData(
        UserAccountAuthenticationData authenticationData)
    {
        var context = new TestContext();
        context.ReadRepository.AllowGetByEmail = true;
        context.ReadRepository.Result = authenticationData;
        return context;
    }

    private static LoginCommand CreateValidCommand()
    {
        return new LoginCommand(
            NormalizedEmail,
            Password);
    }

    private static UserAccountAuthenticationData CreateAuthenticationData(
        bool isUserAccountActive = true,
        bool isEmployeeActive = true,
        string passwordHash = StoredPasswordHash)
    {
        return new UserAccountAuthenticationData(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NormalizedEmail,
            EmployeeRole.Manager,
            isUserAccountActive,
            isEmployeeActive,
            passwordHash);
    }

    private static UserAccount CreateUserAccount(
        UserAccountAuthenticationData authenticationData,
        string? passwordHash = null)
    {
        return new UserAccount(
            authenticationData.EmployeeId,
            passwordHash ?? authenticationData.PasswordHash)
        {
            Id = authenticationData.UserAccountId
        };
    }

    private sealed class TestContext
    {
        public TestContext()
        {
            ReadRepository =
                new FakeUserAccountReadRepository(CallSequence);

            WriteRepository =
                new FakeUserAccountWriteRepository(CallSequence);

            PasswordHashService =
                new FakePasswordHashService(CallSequence);

            JwtTokenGenerator =
                new FakeJwtTokenGenerator(CallSequence);

            Handler =
                new LoginCommandHandler(
                    ReadRepository,
                    WriteRepository,
                    PasswordHashService,
                    JwtTokenGenerator);
        }

        public List<string> CallSequence { get; } = new();

        public FakeUserAccountReadRepository ReadRepository { get; }

        public FakeUserAccountWriteRepository WriteRepository { get; }

        public FakePasswordHashService PasswordHashService { get; }

        public FakeJwtTokenGenerator JwtTokenGenerator { get; }

        public LoginCommandHandler Handler { get; }
    }

    private sealed class FakeUserAccountReadRepository(
        List<string> callSequence)
        : IUserAccountReadRepository
    {
        public bool AllowGetByEmail { get; set; }

        public UserAccountAuthenticationData? Result { get; set; }

        public int GetByEmailCallCount { get; private set; }

        public List<string> RequestedNormalizedEmails { get; } = new();

        public List<CancellationToken> ReceivedCancellationTokens { get; } =
            new();

        public Task<UserAccountAuthenticationData?> GetByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            if (!AllowGetByEmail)
            {
                throw new InvalidOperationException(
                    "Unexpected repository call.");
            }

            GetByEmailCallCount++;
            RequestedNormalizedEmails.Add(normalizedEmail);
            ReceivedCancellationTokens.Add(cancellationToken);
            callSequence.Add("GetByEmail");

            return Task.FromResult(Result);
        }
    }

    private sealed class FakeUserAccountWriteRepository(
        List<string> callSequence)
        : IUserAccountWriteRepository
    {
        public bool AllowGetForUpdate { get; set; }

        public bool AllowSaveChanges { get; set; }

        public UserAccount? Result { get; set; }

        public int GetForUpdateCallCount { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public List<Guid> RequestedUserAccountIds { get; } = new();

        public List<CancellationToken> GetForUpdateTokens { get; } = new();

        public List<CancellationToken> SaveChangesTokens { get; } = new();

        public Task<UserAccount?> GetForUpdateAsync(
            Guid userAccountId,
            CancellationToken cancellationToken = default)
        {
            if (!AllowGetForUpdate)
            {
                throw new InvalidOperationException(
                    "Unexpected repository call.");
            }

            GetForUpdateCallCount++;
            RequestedUserAccountIds.Add(userAccountId);
            GetForUpdateTokens.Add(cancellationToken);
            callSequence.Add("GetForUpdate");

            return Task.FromResult(Result);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            if (!AllowSaveChanges)
            {
                throw new InvalidOperationException(
                    "Unexpected repository call.");
            }

            SaveChangesCallCount++;
            SaveChangesTokens.Add(cancellationToken);
            callSequence.Add("SaveChanges");

            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHashService(
        List<string> callSequence)
        : IPasswordHashService
    {
        public bool AllowVerifyPassword { get; set; }

        public bool AllowHashPassword { get; set; }

        public PasswordVerificationOutcome VerificationOutcome { get; set; }

        public string HashResult { get; set; } = NewPasswordHash;

        public int VerifyPasswordCallCount { get; private set; }

        public int HashPasswordCallCount { get; private set; }

        public List<string> VerifiedPasswordHashes { get; } = new();

        public List<string> VerifiedProvidedPasswords { get; } = new();

        public List<string> HashedPasswords { get; } = new();

        public string HashPassword(string password)
        {
            if (!AllowHashPassword)
            {
                throw new InvalidOperationException(
                    "Unexpected password hash service call.");
            }

            HashPasswordCallCount++;
            HashedPasswords.Add(password);
            callSequence.Add("HashPassword");

            return HashResult;
        }

        public PasswordVerificationOutcome VerifyPassword(
            string passwordHash,
            string providedPassword)
        {
            if (!AllowVerifyPassword)
            {
                throw new InvalidOperationException(
                    "Unexpected password hash service call.");
            }

            VerifyPasswordCallCount++;
            VerifiedPasswordHashes.Add(passwordHash);
            VerifiedProvidedPasswords.Add(providedPassword);
            callSequence.Add("VerifyPassword");

            return VerificationOutcome;
        }
    }

    private sealed class FakeJwtTokenGenerator(
        List<string> callSequence)
        : IJwtTokenGenerator
    {
        public bool AllowGenerateToken { get; set; }

        public JwtTokenResult Result { get; set; } =
            new(
                "generated-access-token",
                TokenExpiresAtUtc);

        public int GenerateTokenCallCount { get; private set; }

        public List<JwtTokenRequest> RequestedTokenRequests { get; } = new();

        public JwtTokenResult GenerateToken(
            JwtTokenRequest request)
        {
            if (!AllowGenerateToken)
            {
                throw new InvalidOperationException(
                    "Unexpected token generator call.");
            }

            GenerateTokenCallCount++;
            RequestedTokenRequests.Add(request);
            callSequence.Add("GenerateToken");

            return Result;
        }
    }
}
