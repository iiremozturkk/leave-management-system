namespace LeaveManagementSystem.Application.Common.Exceptions;

public sealed class BusinessRuleException(string message)
    : Exception(message);
