using TrustFix.Core.Domain;

namespace TrustFix.Engine;

public sealed class TrustFixEngine
{
    public EvaluationReport Evaluate(RawSnapshot s)
    {
        var findings = new List<Finding>();

        // Findings (rules v1)
        if (s.DiskFreePercent < 10)
        {
            findings.Add(new Finding(
                Id: "storage.low_free_space",
                Title: "Low disk space on system drive",
                Description: "Low free space can slow down the system and cause update failures.",
                Severity: Severity.Critical,
                Evidence: $"Free space is {s.DiskFreePercent:0.0}% (~{FormatBytes(s.DiskFreeBytes)}).",
                Recommendation: "Run Safe Cleanup (temp/cache/recycle bin) and move large files off the system drive.",
                Impact: "High",
                ReclaimableBytes: null
            ));
        }

        if (s.StartupAppCount > 12)
        {
            findings.Add(new Finding(
                Id: "startup.too_many_apps",
                Title: "Too many apps start with Windows",
                Description: "Excess startup apps increase boot time and waste RAM/CPU in the background.",
                Severity: Severity.Warning,
                Evidence: $"Detected {s.StartupAppCount} startup apps.",
                Recommendation: "Disable non-essential startup apps (keep drivers/security tools enabled).",
                Impact: "Medium"
            ));
        }

        if (s.RamUsedPercent > 85)
        {
            findings.Add(new Finding(
                Id: "memory.high_usage",
                Title: "High memory usage",
                Description: "When RAM is near full, Windows starts paging to disk, making everything feel slow.",
                Severity: s.RamUsedPercent > 92 ? Severity.Critical : Severity.Warning,
                Evidence: $"RAM usage is {s.RamUsedPercent:0.0}%.",
                Recommendation: "Close heavy apps/tabs, reduce background apps, and consider upgrading RAM if this is frequent.",
                Impact: "High"
            ));
        }

        // Scoring (simple, explainable)
        var perf = 100;
        if (s.DiskFreePercent < 10) perf -= 30;
        if (s.StartupAppCount > 12) perf -= 10;
        if (s.RamUsedPercent > 85) perf -= (s.RamUsedPercent > 92 ? 20 : 10);
        perf = Clamp(perf);

        // v1: Stability/Security are conservative placeholders
        var stability = Clamp(90 - (s.DiskFreePercent < 10 ? 10 : 0));
        var security = 85;

        return new EvaluationReport(new ScoreSummary(perf, stability, security), findings);
    }

    private static int Clamp(int v) => Math.Max(0, Math.Min(100, v));

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        return $"{v:0.#} {units[i]}";
    }
}
