using LeaveManagementSystem.Application.Authentication.Models;
using MediatR;

namespace LeaveManagementSystem.Application.Authentication.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password)
    : IRequest<LoginResult?>;
