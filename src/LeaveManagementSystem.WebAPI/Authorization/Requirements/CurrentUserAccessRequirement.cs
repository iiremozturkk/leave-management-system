using LeaveManagementSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace LeaveManagementSystem.WebAPI.Authorization.Requirements;

public sealed record CurrentUserAccessRequirement(
    EmployeeRole? RequiredRole = null)
    : IAuthorizationRequirement;
