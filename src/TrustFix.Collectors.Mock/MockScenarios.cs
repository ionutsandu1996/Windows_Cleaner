using TrustFix.Core.Domain;

namespace TrustFix.Collectors.Mock;

public static class MockScenarios
{
    public static RawSnapshot SlowLaptop() => new()
    {
        DiskFreePercent = 7.8,
        DiskFreeBytes = 12L * 1024 * 1024 * 1024, // 12 GB
        StartupAppCount = 18,
        RamUsedPercent = 82.5
    };

    public static RawSnapshot RamPressure() => new()
    {
        DiskFreePercent = 22.0,
        DiskFreeBytes = 80L * 1024 * 1024 * 1024,
        StartupAppCount = 6,
        RamUsedPercent = 92.0
    };
}
