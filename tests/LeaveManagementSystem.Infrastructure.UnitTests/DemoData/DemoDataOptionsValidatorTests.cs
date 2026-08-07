using LeaveManagementSystem.Infrastructure.DemoData;

namespace LeaveManagementSystem.Infrastructure.UnitTests.DemoData;

public sealed class DemoDataOptionsValidatorTests
{
    private readonly DemoDataOptionsValidator _validator =
        new();

    [Fact]
    public void Validate_WhenSeedingEnabledWithoutPassword_ReturnsFailure()
    {
        var options =
            new DemoDataOptions
            {
                SeedOnStartup = true,
                Password = string.Empty
            };

        var result =
            _validator.Validate(
                name: null,
                options);

        Assert.True(
            result.Failed);

        Assert.Contains(
            "DemoData:Password is required when demo data seeding is enabled.",
            result.Failures);
    }
}
