using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.Infrastructure.Persistence;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.LeaveRequests;

public sealed class GetLeaveBalanceEndpointTests(
    TestWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetBalance_ValidEmployeeAndLeaveType_ReturnsCurrentBalance()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            using var response =
                await GetBalanceAsync(
                    employeeId.Value,
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
                employeeId.Value,
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
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task GetBalance_EmployeeIdIsEmpty_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await GetBalanceAsync(
                Guid.Empty,
                AnnualLeaveTypeId,
                2026);

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Employee id cannot be empty.");
    }

    [Fact]
    public async Task GetBalance_LeaveTypeIdIsEmpty_ReturnsBadRequest()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await GetBalanceAsync(
                Guid.NewGuid(),
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
                Guid.NewGuid(),
                AnnualLeaveTypeId,
                year);

        await AssertInvalidLeaveRequestProblemDetailsAsync(
            response,
            "Year must be between 2000 and 2100.");
    }

    [Fact]
    public async Task GetBalance_EmployeeDoesNotExist_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await GetBalanceAsync(
                Guid.NewGuid(),
                AnnualLeaveTypeId,
                2026);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetBalance_EmployeeIsInactive_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            await DeactivateEmployeeAsync(
                employeeId.Value);

            using var response =
                await GetBalanceAsync(
                    employeeId.Value,
                    AnnualLeaveTypeId,
                    2026);

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task GetBalance_LeaveTypeDoesNotExist_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            using var response =
                await GetBalanceAsync(
                    employeeId.Value,
                    Guid.NewGuid(),
                    2026);

            Assert.Equal(
                HttpStatusCode.NotFound,
                response.StatusCode);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: null,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    private async Task<HttpResponseMessage> GetBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year)
    {
        return await _client.GetAsync(
            $"/api/leave-requests/balance" +
            $"?employeeId={employeeId}" +
            $"&leaveTypeId={leaveTypeId}" +
            $"&year={year}");
    }

    private async Task DeactivateEmployeeAsync(
        Guid employeeId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var employee =
            await dbContext.Employees
                .SingleAsync(
                    employee =>
                        employee.Id == employeeId);

        employee.IsActive = false;

        await dbContext.SaveChangesAsync();
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
