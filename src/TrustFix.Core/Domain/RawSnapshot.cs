namespace TrustFix.Core.Domain;

public sealed record RawSnapshot
{
    public DateTime CapturedAtUtc { get; init; } = DateTime.UtcNow;

    // Storage
    public double DiskFreePercent { get; init; }           // 0..100
    public long DiskFreeBytes { get; init; }               // optional, but useful

    // Startup
    public int StartupAppCount { get; init; }

    // Memory
    public double RamUsedPercent { get; init; }            // 0..100
}
