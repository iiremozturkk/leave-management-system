namespace LeaveManagementSystem.Application.Common.Exceptions;

public sealed class ForbiddenOperationException : InvalidOperationException
{
    public ForbiddenOperationException(string message)
        : base(message)
    {
    }
}