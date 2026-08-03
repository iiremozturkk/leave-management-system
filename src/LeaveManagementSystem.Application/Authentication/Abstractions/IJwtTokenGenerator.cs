using LeaveManagementSystem.Application.Authentication.Models;

namespace LeaveManagementSystem.Application.Authentication.Abstractions;

public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateToken(
        JwtTokenRequest request);
}
