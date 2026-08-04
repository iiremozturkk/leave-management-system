using LeaveManagementSystem.Application.Authentication.Abstractions;
using LeaveManagementSystem.Application.Authentication.Models;
using LeaveManagementSystem.Application.Authentication.Services;
using LeaveManagementSystem.Domain.Enums;
using Xunit;

namespace LeaveManagementSystem.Application.UnitTests.Authentication.CurrentUserAccess;

public sealed class CurrentUserAccessServiceTests
{
    private static readonly Guid UserAccountId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    private static readonly Guid EmployeeId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    private static readonly Guid OtherUserAccountId =
        Guid.Parse(
            "33333333-3333-3333-3333-333333333333");

    private static readonly Guid OtherEmployeeId =
        Guid.Parse(
            "44444444-4444-4444-4444-444444444444");

    private const string ClaimEmail =
        "employee@example.com";

    [Fact]
    public void Constructor_WithNullCurrentUser_ThrowsArgumentNullException()
    {
        var repository =
            new FakeCurrentUserAccessReadRepository();

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new CurrentUserAccessService(
                    null!,
                    repository));

        Assert.Equal(
            "currentUser",
            exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        var currentUser =
            CreateValidCurrentUser();

        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new CurrentUserAccessService(
                    currentUser,
                    null!));

        Assert.Equal(
            "currentUserAccessReadRepository",
            exception.ParamName);
    }

    [Fact]
    public async Task GetAsync_WithUnauthenticatedUser_ReturnsNullWithoutRepositoryCall()
    {
        using var context =
            new TestContext();

        context.CurrentUser.IsAuthenticated =
            false;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);

        Assert.Equal(
            0,
            context.Repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_WithMissingUserAccountId_ReturnsNullWithoutRepositoryCall()
    {
        using var context =
            new TestContext();

        context.CurrentUser.UserAccountId =
            null;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);

        Assert.Equal(
            0,
            context.Repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_WithMissingEmployeeId_ReturnsNullWithoutRepositoryCall()
    {
        using var context =
            new TestContext();

        context.CurrentUser.EmployeeId =
            null;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);

        Assert.Equal(
            0,
            context.Repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_WithMissingRole_ReturnsNullWithoutRepositoryCall()
    {
        using var context =
            new TestContext();

        context.CurrentUser.Role =
            null;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);

        Assert.Equal(
            0,
            context.Repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_WithNullEmail_ReturnsNullWithoutRepositoryCall()
    {
        using var context =
            new TestContext();

        context.CurrentUser.Email =
            null;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);

        Assert.Equal(
            0,
            context.Repository.CallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_WithEmptyOrWhitespaceEmail_ReturnsNullWithoutRepositoryCall(
        string email)
    {
        using var context =
            new TestContext();

        context.CurrentUser.Email =
            email;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);

        Assert.Equal(
            0,
            context.Repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_WhenUserAccountDoesNotExist_ReturnsNull()
    {
        using var context =
            new TestContext();

        context.Repository.Handler =
            (_, _) =>
                Task.FromResult<CurrentUserAccessData?>(
                    null);

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);

        Assert.Equal(
            1,
            context.Repository.CallCount);

        Assert.Equal(
            UserAccountId,
            Assert.Single(
                context.Repository.RequestedUserAccountIds));
    }

    [Fact]
    public async Task GetAsync_WithMismatchedUserAccountId_ReturnsNull()
    {
        using var context =
            new TestContext();

        var accessData =
            CreateMatchingAccessData(
                context.CurrentUser)
            with
            {
                UserAccountId =
                    OtherUserAccountId
            };

        context.Repository.Result =
            accessData;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetAsync_WithMismatchedEmployeeId_ReturnsNull()
    {
        using var context =
            new TestContext();

        var accessData =
            CreateMatchingAccessData(
                context.CurrentUser)
            with
            {
                EmployeeId =
                    OtherEmployeeId
            };

        context.Repository.Result =
            accessData;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetAsync_WithInactiveUserAccount_ReturnsNull()
    {
        using var context =
            new TestContext();

        var accessData =
            CreateMatchingAccessData(
                context.CurrentUser)
            with
            {
                IsUserAccountActive =
                    false
            };

        context.Repository.Result =
            accessData;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetAsync_WithInactiveEmployee_ReturnsNull()
    {
        using var context =
            new TestContext();

        var accessData =
            CreateMatchingAccessData(
                context.CurrentUser)
            with
            {
                IsEmployeeActive =
                    false
            };

        context.Repository.Result =
            accessData;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetAsync_WithStaleRoleClaim_ReturnsNull()
    {
        using var context =
            new TestContext();

        var accessData =
            CreateMatchingAccessData(
                context.CurrentUser)
            with
            {
                Role =
                    EmployeeRole.HR
            };

        context.Repository.Result =
            accessData;

        var result =
            await context.Service.GetAsync();

        Assert.Null(
            result);
    }

    [Fact]
    public async Task GetAsync_WithValidCurrentAccess_ReturnsDatabaseAccessData()
    {
        using var context =
            new TestContext();

        var accessData =
            CreateMatchingAccessData(
                context.CurrentUser);

        context.Repository.Result =
            accessData;

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var result =
            await context.Service.GetAsync(
                cancellationToken);

        Assert.NotNull(
            result);

        Assert.Equal(
            accessData.UserAccountId,
            result.UserAccountId);

        Assert.Equal(
            accessData.EmployeeId,
            result.EmployeeId);

        Assert.Equal(
            accessData.Email,
            result.Email);

        Assert.Equal(
            accessData.Role,
            result.Role);

        Assert.Equal(
            UserAccountId,
            Assert.Single(
                context.Repository.RequestedUserAccountIds));

        Assert.Equal(
            cancellationToken,
            Assert.Single(
                context.Repository.ReceivedCancellationTokens));
    }

    [Fact]
    public async Task GetAsync_WithDifferentDatabaseEmail_ReturnsCurrentDatabaseEmail()
    {
        using var context =
            new TestContext();

        const string currentDatabaseEmail =
            "current.database@example.com";

        context.Repository.Result =
            CreateMatchingAccessData(
                context.CurrentUser)
            with
            {
                Email =
                    currentDatabaseEmail
            };

        var result =
            await context.Service.GetAsync();

        Assert.NotNull(
            result);

        Assert.Equal(
            currentDatabaseEmail,
            result.Email);
    }

    [Fact]
    public async Task GetAsync_AfterSuccessfulResolution_ReturnsCachedResult()
    {
        using var context =
            new TestContext();

        context.Repository.Result =
            CreateMatchingAccessData(
                context.CurrentUser);

        var firstResult =
            await context.Service.GetAsync();

        var secondResult =
            await context.Service.GetAsync();

        Assert.NotNull(
            firstResult);

        Assert.Same(
            firstResult,
            secondResult);

        Assert.Equal(
            1,
            context.Repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_AfterNullResolution_ReturnsCachedNull()
    {
        using var context =
            new TestContext();

        context.Repository.Handler =
            (_, _) =>
                Task.FromResult<CurrentUserAccessData?>(
                    null);

        var firstResult =
            await context.Service.GetAsync();

        var secondResult =
            await context.Service.GetAsync();

        Assert.Null(
            firstResult);

        Assert.Null(
            secondResult);

        Assert.Equal(
            1,
            context.Repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_WithConcurrentCalls_PerformsSingleRepositoryQuery()
    {
        using var context =
            new TestContext();

        var accessData =
            CreateMatchingAccessData(
                context.CurrentUser);

        var repositoryEntered =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        var releaseRepository =
            new TaskCompletionSource<CurrentUserAccessData?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        context.Repository.Handler =
            (_, _) =>
            {
                repositoryEntered.TrySetResult(
                    true);

                return releaseRepository.Task;
            };

        var firstTask =
            context.Service.GetAsync();

        await repositoryEntered.Task;

        var secondTask =
            context.Service.GetAsync();

        Assert.Equal(
            1,
            context.Repository.CallCount);

        releaseRepository.SetResult(
            accessData);

        var results =
            await Task.WhenAll(
                firstTask,
                secondTask);

        Assert.NotNull(
            results[0]);

        Assert.Same(
            results[0],
            results[1]);

        Assert.Equal(
            1,
            context.Repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_WhenRepositoryFails_DoesNotCacheFailure()
    {
        using var context =
            new TestContext();

        var accessData =
            CreateMatchingAccessData(
                context.CurrentUser);

        var attemptNumber =
            0;

        context.Repository.Handler =
            (_, _) =>
            {
                attemptNumber++;

                if (attemptNumber == 1)
                {
                    return Task.FromException<CurrentUserAccessData?>(
                        new InvalidOperationException(
                            "Repository failure."));
                }

                return Task.FromResult<CurrentUserAccessData?>(
                    accessData);
            };

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.Service.GetAsync());

        Assert.Equal(
            "Repository failure.",
            exception.Message);

        var result =
            await context.Service.GetAsync();

        Assert.NotNull(
            result);

        Assert.Equal(
            2,
            context.Repository.CallCount);
    }

    [Fact]
    public async Task GetAsync_WhenRepositoryIsCanceled_DoesNotCacheCancellation()
    {
        using var context =
            new TestContext();

        var accessData =
            CreateMatchingAccessData(
                context.CurrentUser);

        var attemptNumber =
            0;

        context.Repository.Handler =
            (_, _) =>
            {
                attemptNumber++;

                if (attemptNumber == 1)
                {
                    return Task.FromCanceled<CurrentUserAccessData?>(
                        new CancellationToken(
                            canceled: true));
                }

                return Task.FromResult<CurrentUserAccessData?>(
                    accessData);
            };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.GetAsync());

        var result =
            await context.Service.GetAsync();

        Assert.NotNull(
            result);

        Assert.Equal(
            2,
            context.Repository.CallCount);
    }

    private static TestCurrentUser CreateValidCurrentUser()
    {
        return new TestCurrentUser
        {
            IsAuthenticated =
                true,

            UserAccountId =
                UserAccountId,

            EmployeeId =
                EmployeeId,

            Email =
                ClaimEmail,

            Role =
                EmployeeRole.Manager
        };
    }

    private static CurrentUserAccessData CreateMatchingAccessData(
        TestCurrentUser currentUser)
    {
        return new CurrentUserAccessData(
            currentUser.UserAccountId!.Value,
            currentUser.EmployeeId!.Value,
            currentUser.Email!,
            currentUser.Role!.Value,
            IsUserAccountActive: true,
            IsEmployeeActive: true);
    }

    private sealed class TestContext : IDisposable
    {
        public TestContext()
        {
            CurrentUser =
                CreateValidCurrentUser();

            Repository =
                new FakeCurrentUserAccessReadRepository();

            Service =
                new CurrentUserAccessService(
                    CurrentUser,
                    Repository);
        }

        public TestCurrentUser CurrentUser { get; }

        public FakeCurrentUserAccessReadRepository Repository { get; }

        public CurrentUserAccessService Service { get; }

        public void Dispose()
        {
            Service.Dispose();
        }
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; set; }

        public Guid? UserAccountId { get; set; }

        public Guid? EmployeeId { get; set; }

        public string? Email { get; set; }

        public EmployeeRole? Role { get; set; }
    }

    private sealed class FakeCurrentUserAccessReadRepository
        : ICurrentUserAccessReadRepository
    {
        public Func<
            Guid,
            CancellationToken,
            Task<CurrentUserAccessData?>>?
            Handler
        { get; set; }

        public CurrentUserAccessData? Result { get; set; }

        public int CallCount { get; private set; }

        public List<Guid> RequestedUserAccountIds { get; } =
            new();

        public List<CancellationToken> ReceivedCancellationTokens { get; } =
            new();

        public Task<CurrentUserAccessData?> GetByUserAccountIdAsync(
            Guid userAccountId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            RequestedUserAccountIds.Add(
                userAccountId);

            ReceivedCancellationTokens.Add(
                cancellationToken);

            if (Handler is not null)
            {
                return Handler(
                    userAccountId,
                    cancellationToken);
            }

            return Task.FromResult(
                Result);
        }
    }
}
