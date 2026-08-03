namespace LeaveManagementSystem.Application.Authentication.Models;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc);
