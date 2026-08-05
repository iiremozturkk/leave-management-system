using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.LeaveRequests;

public sealed class GetLeaveRequestByIdEndpointTests(
    TestWebApplicationFactory factory)
    : HrIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetLeaveRequestById_LeaveRequestExists_ReturnsProjectedLeaveRequest()
    {
        await EnsureDatabaseReadyAsync();

        var departmentId =
            await CreateDepartmentAsync();

        Guid? employeeId = null;
        Guid? leaveRequestId = null;

        try
        {
            employeeId =
                await CreateEmployeeViaApiAsync(
                    departmentId);

            var createRequest = new
            {
                employeeId = employeeId.Value,
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-11-10",
                endDate = "2026-11-12",
                reason =
                    "Get by id integration test."
            };

            using var createResponse =
                await _client.PostAsJsonAsync(
                    "/api/leave-requests",
                    createRequest);

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var createdLeaveRequest =
                await createResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                createdLeaveRequest);

            leaveRequestId =
                createdLeaveRequest!.Id;

            using var response =
                await _client.GetAsync(
                    $"/api/leave-requests/{leaveRequestId.Value}");

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var leaveRequest =
                await response.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                leaveRequest);

            Assert.Equal(
                leaveRequestId.Value,
                leaveRequest!.Id);

            Assert.Equal(
                employeeId.Value,
                leaveRequest.EmployeeId);

            Assert.Equal(
                "Integration LeaveEmployee",
                leaveRequest.EmployeeFullName);

            Assert.Equal(
                AnnualLeaveTypeId,
                leaveRequest.LeaveTypeId);

            Assert.Equal(
                "Annual Leave",
                leaveRequest.LeaveTypeName);

            Assert.Equal(
                new DateOnly(2026, 11, 10),
                leaveRequest.StartDate);

            Assert.Equal(
                new DateOnly(2026, 11, 12),
                leaveRequest.EndDate);

            Assert.Equal(
                3,
                leaveRequest.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                leaveRequest.Status);

            Assert.Equal(
                "Get by id integration test.",
                leaveRequest.Reason);

            Assert.Null(
                leaveRequest.ManagerComment);

            Assert.Null(
                leaveRequest.ReviewedAtUtc);

            Assert.Null(
                leaveRequest.ReviewedByEmployeeId);

            Assert.Null(
                leaveRequest.ReviewedByEmployeeFullName);

            Assert.NotEqual(
                default,
                leaveRequest.CreatedAtUtc);

            Assert.Null(
                leaveRequest.UpdatedAtUtc);
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: leaveRequestId,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }

    [Fact]
    public async Task GetLeaveRequestById_LeaveRequestDoesNotExist_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        var leaveRequestId =
            Guid.NewGuid();

        using var response =
            await _client.GetAsync(
                $"/api/leave-requests/{leaveRequestId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetLeaveRequestById_IdIsEmpty_ReturnsNotFound()
    {
        await EnsureDatabaseReadyAsync();

        using var response =
            await _client.GetAsync(
                $"/api/leave-requests/{Guid.Empty}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}
