using NetWorthTracker.Core.Enums;

namespace NetWorthTracker.Infrastructure.Services;

public class AppSettings
{
    public AppMode AppMode { get; set; } = AppMode.SelfHosted;
}
