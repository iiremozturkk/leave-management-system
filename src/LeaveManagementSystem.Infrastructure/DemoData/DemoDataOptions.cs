namespace LeaveManagementSystem.Infrastructure.DemoData;

public sealed class DemoDataOptions
{
    public const string SectionName =
        "DemoData";

    public bool SeedOnStartup { get; set; }

    public string Password { get; set; } =
        string.Empty;
}
