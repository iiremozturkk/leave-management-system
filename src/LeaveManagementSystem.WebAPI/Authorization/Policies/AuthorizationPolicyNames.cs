namespace LeaveManagementSystem.WebAPI.Authorization.Policies;

public static class AuthorizationPolicyNames
{
    public const string AuthenticatedEmployee =
        nameof(AuthenticatedEmployee);

    public const string HrOnly =
        nameof(HrOnly);

    public const string ManagerOnly =
        nameof(ManagerOnly);
}
