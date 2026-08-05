using System.Net;
using System.Net.Http.Json;
using LeaveManagementSystem.Domain.Enums;
using LeaveManagementSystem.IntegrationTests.Contracts;
using LeaveManagementSystem.IntegrationTests.Infrastructure;
using Xunit;

namespace LeaveManagementSystem.IntegrationTests.LeaveRequests;

public sealed class LeaveRequestCrudEndpointTests(
    TestWebApplicationFactory factory)
    : HrIntegrationTestBase(factory)
{
    [Fact]
    public async Task LeaveRequestCrudFlow_WorksThroughApi()
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

            var listBeforeCreateResponse =
                await _client.GetAsync(
                    "/api/leave-requests");

            Assert.Equal(
                HttpStatusCode.OK,
                listBeforeCreateResponse.StatusCode);

            var createRequest = new
            {
                employeeId,
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-07-15",
                endDate = "2026-07-17",
                reason =
                    "Integration test leave request."
            };

            var createResponse =
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

            Assert.Equal(
                employeeId,
                createdLeaveRequest.EmployeeId);

            Assert.Equal(
                AnnualLeaveTypeId,
                createdLeaveRequest.LeaveTypeId);

            Assert.Equal(
                3,
                createdLeaveRequest.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                createdLeaveRequest.Status);

            Assert.Equal(
                "Integration test leave request.",
                createdLeaveRequest.Reason);

            var getByIdResponse =
                await _client.GetAsync(
                    $"/api/leave-requests/{leaveRequestId}");

            Assert.Equal(
                HttpStatusCode.OK,
                getByIdResponse.StatusCode);

            var loadedLeaveRequest =
                await getByIdResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                loadedLeaveRequest);

            Assert.Equal(
                leaveRequestId,
                loadedLeaveRequest!.Id);

            var listAfterCreateResponse =
                await _client.GetAsync(
                    "/api/leave-requests");

            Assert.Equal(
                HttpStatusCode.OK,
                listAfterCreateResponse.StatusCode);

            var leaveRequests =
                await listAfterCreateResponse.Content
                    .ReadFromJsonAsync<List<LeaveRequestResponse>>(
                        JsonOptions);

            Assert.NotNull(
                leaveRequests);

            Assert.Contains(
                leaveRequests!,
                leaveRequest =>
                    leaveRequest.Id == leaveRequestId);

            var updateRequest = new
            {
                leaveTypeId = AnnualLeaveTypeId,
                startDate = "2026-07-16",
                endDate = "2026-07-18",
                reason =
                    "Updated integration test leave request."
            };

            var updateResponse =
                await _client.PutAsJsonAsync(
                    $"/api/leave-requests/{leaveRequestId}",
                    updateRequest);

            Assert.Equal(
                HttpStatusCode.OK,
                updateResponse.StatusCode);

            var updatedLeaveRequest =
                await updateResponse.Content
                    .ReadFromJsonAsync<LeaveRequestResponse>(
                        JsonOptions);

            Assert.NotNull(
                updatedLeaveRequest);

            Assert.Equal(
                3,
                updatedLeaveRequest!.RequestedDays);

            Assert.Equal(
                LeaveRequestStatus.Pending,
                updatedLeaveRequest.Status);

            Assert.Equal(
                "Updated integration test leave request.",
                updatedLeaveRequest.Reason);

            var deleteResponse =
                await _client.DeleteAsync(
                    $"/api/leave-requests/{leaveRequestId}");

            Assert.Equal(
                HttpStatusCode.NoContent,
                deleteResponse.StatusCode);

            leaveRequestId = null;
        }
        finally
        {
            await CleanupAsync(
                leaveRequestId: leaveRequestId,
                employeeId: employeeId,
                departmentId: departmentId);
        }
    }
}
