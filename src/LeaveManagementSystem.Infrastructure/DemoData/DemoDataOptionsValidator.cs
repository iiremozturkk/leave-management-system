using Microsoft.Extensions.Options;

namespace LeaveManagementSystem.Infrastructure.DemoData;

public sealed class DemoDataOptionsValidator
    : IValidateOptions<DemoDataOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        DemoDataOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.SeedOnStartup &&
            string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Fail(
                "DemoData:Password is required when demo data seeding is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}
