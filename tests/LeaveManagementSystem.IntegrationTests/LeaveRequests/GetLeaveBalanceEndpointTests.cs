using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.LeaveRequests;

public sealed class GetLeaveBalanceEndpointTests(
    TestWebApplicationFactory factory)
    : HrIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetBalance_ValidLeaveType_ReturnsCurrentUsersBalance()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await GetBalanceAsync(
                AnnualLeaveTypeId,
                2026);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var balance =
            await response.Content
                .ReadFromJsonAsync<LeaveBalanceResponse>(
                    JsonOptions);

        Assert.NotNull(
            balance);

        Assert.Equal(
            HrEmployeeId,
            balance!.EmployeeId);

        Assert.Equal(
            AnnualLeaveTypeId,
            balance.LeaveTypeId);

        Assert.Equal(
            "Annual Leave",
            balance.LeaveTypeName);

        Assert.Equal(
            2026,
            balance.Year);

        Assert.Equal(
            20,
            balance.EntitledDays);

        Assert.Equal(
            0,
            balance.UsedDays);

        Assert.Equal(
            20,
            balance.RemainingDays);
    }

    [Fact]
    public async Task GetBalance_WithoutToken_ReturnsUnauthorized()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await _client.GetAsync(
                "/api/leave-requests/balance" +
                $"?leaveTypeId={AnnualLeaveTypeId}" +
                "&year=2026");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetBalance_LeaveTypeIdIsEmpty_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await GetBalanceAsync(
                Guid.Empty,
                2026);

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Leave type id cannot be empty.");
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task GetBalance_YearIsOutsideSupportedRange_ReturnsBadRequest(
        int year)
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await GetBalanceAsync(
                AnnualLeaveTypeId,
                year);

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Year must be between 2000 and 2100.");
    }

    [Fact]
    public async Task GetBalance_EmployeeIdQueryParameterCannotOverrideCurrentUser()
    {
        await EnsureDatabaseReadyAsync();

        var otherEmployeeId =
            Guid.NewGuid();

        using var response =
            await HrClient.GetAsync(
                "/api/leave-requests/balance" +
                $"?employeeId={otherEmployeeId}" +
                $"&leaveTypeId={AnnualLeaveTypeId}" +
                "&year=2026");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var balance =
            await response.Content
                .ReadFromJsonAsync<LeaveBalanceResponse>(
                    JsonOptions);

        Assert.NotNull(
            balance);

        Assert.Equal(
            HrEmployeeId,
            balance!.EmployeeId);

        Assert.NotEqual(
            otherEmployeeId,
            balance.EmployeeId);
    }

    [Fact]
    public async Task GetBalance_YearIsMissing_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await HrClient.GetAsync(
                "/api/leave-requests/balance" +
                $"?leaveTypeId={AnnualLeaveTypeId}");

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Year must be between 2000 and 2100.");
    }

    [Fact]
    public async Task GetBalance_LeaveTypeDoesNotExist_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await GetBalanceAsync(
                Guid.NewGuid(),
                2026);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private async Task<HttpResponseMessage> GetBalanceAsync(
        Guid leaveTypeId,
        int year)
    {
        return await HrClient.GetAsync(
            "/api/leave-requests/balance" +
            $"?leaveTypeId={leaveTypeId}" +
            $"&year={year}");
    }

    private static async Task AssertInvalidLeaveRequestProblemDetailsAsync(
        HttpResponseMessage response,
        string expectedDetail)
    {
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetailsResponse>(
                    JsonOptions);

        Assert.NotNull(
            problem);

        Assert.Equal(
            400,
            problem!.Status);

        Assert.Equal(
            "Invalid leave request.",
            problem.Title);

        Assert.Equal(
            expectedDetail,
            problem.Detail);
    }
}
