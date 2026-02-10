using System.Net;
using System.Text;
using TrustFix.Core.Domain;
using TrustFix.Engine;

namespace TrustFix.Reporting;

public static class HtmlReportGenerator
{
    public static string Generate(EvaluationReport report, RawSnapshot snapshot)
    {
        static string H(string s) => WebUtility.HtmlEncode(s);

        static string ScoreClass(int score)
        {
            if (score >= 80) return "good";
            if (score >= 60) return "ok";
            return "bad";
        }

        static string OverallTitle(string cls) => cls switch
        {
            "good" => "All good",
            "ok" => "Needs attention",
            _ => "Action recommended"
        };

        static string OverallSubtitle(string cls) => cls switch
        {
            "good" => "Your system looks healthy. Keep it up.",
            "ok" => "Your system is usable, but a few fixes will improve it.",
            _ => "Your system needs fixes to avoid slowdowns or issues."
        };

        static (string Label, string ClassName) ImpactEstimate(string findingId)
        {
            return findingId switch
            {
                "storage.low_free_space" =>
                    ("High impact: smoother system & fewer update errors", "impact-high"),

                "startup.too_many_apps" =>
                    ("Medium impact: faster boot & less background load", "impact-medium"),

                "memory.high_usage" =>
                    ("High impact: fewer freezes & better responsiveness", "impact-high"),

                _ =>
                    ("Low impact: small improvement", "impact-low")
            };
        }

        // --- VALUE-BASED ESTIMATED IMPROVEMENT (v1 heuristic)
        // Returns estimated performance points gained if user applies recommended fixes.
        static int EstimateImprovement(RawSnapshot s)
        {
            var gain = 0;

            // Disk free: biggest “feel” improvement when very low.
            // If < 5% => +25, 5–10% => +20, 10–15% => +12, 15–20% => +6, >=20% => +0
            if (s.DiskFreePercent < 5) gain += 25;
            else if (s.DiskFreePercent < 10) gain += 20;
            else if (s.DiskFreePercent < 15) gain += 12;
            else if (s.DiskFreePercent < 20) gain += 6;

            // Startup apps: baseline “healthy” is ~8-10.
            // Every app above 10 adds a little gain, capped.
            if (s.StartupAppCount > 10)
            {
                var extra = s.StartupAppCount - 10;         // e.g. 18 => 8
                gain += Math.Min(14, extra * 2);            // 8*2=16 => cap 14
            }

            // RAM used: if >85% the system tends to stutter.
            // 85–90 => +6, 90–95 => +12, >95 => +18
            if (s.RamUsedPercent > 95) gain += 18;
            else if (s.RamUsedPercent > 90) gain += 12;
            else if (s.RamUsedPercent > 85) gain += 6;

            // Keep it believable for MVP
            return Math.Min(gain, 40);
        }

        var worstScore = Math.Min(
            report.Scores.Performance,
            Math.Min(report.Scores.Stability, report.Scores.Security)
        );
        var overallClass = ScoreClass(worstScore);

        var improvement = EstimateImprovement(snapshot);
        var estimatedPerfAfter = Math.Min(100, report.Scores.Performance + improvement);

        var sb = new StringBuilder();

        sb.Append($@"
<!doctype html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>TrustFix – System Health Report</title>

<style>
body {{
  font-family: Arial, sans-serif;
  background:#f5f7fa;
  padding:30px;
}}

.card {{
  background:#fff;
  border-radius:14px;
  padding:20px;
  margin-bottom:18px;
  box-shadow:0 1px 10px rgba(0,0,0,.06);
}}

h1 {{ margin:0 0 8px 0; }}
h2 {{ margin:0 0 12px 0; }}
h3 {{ margin:0 0 6px 0; }}

.meta {{ color:#666; }}

.actionbar {{
  position: sticky;
  top: 14px;
  z-index: 999;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  padding: 10px;
  border-radius: 14px;
  background: rgba(255,255,255,0.9);
  backdrop-filter: blur(8px);
  box-shadow:0 1px 10px rgba(0,0,0,.06);
  margin-bottom:18px;
}}

.btn {{
  text-decoration:none;
  padding:10px 14px;
  border-radius:10px;
  font-weight:800;
  background:#111;
  color:#fff;
}}

.btn-secondary {{ background:#444; }}

.overall {{
  border-left:12px solid #ccc;
}}
.overall.good {{ border-left-color:#27ae60; background:#f3fbf6; }}
.overall.ok   {{ border-left-color:#f1c40f; background:#fffaf0; }}
.overall.bad  {{ border-left-color:#e74c3c; background:#fff4f4; }}

.overall-title {{
  font-size:22px;
  font-weight:800;
}}

.pill {{
  display:inline-block;
  padding:6px 10px;
  border-radius:999px;
  font-weight:900;
  font-size:12px;
  background:#eee;
}}

.pill.good {{ background:#dff3e6; }}
.pill.ok   {{ background:#fff2c2; }}
.pill.bad  {{ background:#ffd6d6; }}

.kpi-row {{
  display:flex;
  flex-wrap:wrap;
  gap:12px;
  margin-top:10px;
}}

.kpi {{
  background: rgba(255,255,255,0.7);
  border: 1px solid rgba(0,0,0,0.06);
  border-radius: 12px;
  padding: 12px 14px;
  min-width: 240px;
}}

.kpi-label {{ color:#666; font-size:13px; }}
.kpi-value {{ margin-top:6px; font-weight:900; font-size:18px; }}
.kpi-sub {{ margin-top:4px; color:#666; font-size:12px; }}

.scores {{
  display:flex;
  gap:16px;
  flex-wrap:wrap;
}}

.scorebox {{
  flex:1;
  min-width:220px;
  border-left:10px solid #ccc;
}}

.scorebox.good {{ border-left-color:#27ae60; background:#f3fbf6; }}
.scorebox.ok   {{ border-left-color:#f1c40f; background:#fffaf0; }}
.scorebox.bad  {{ border-left-color:#e74c3c; background:#fff4f4; }}

.score {{
  font-size:30px;
  font-weight:900;
}}

.label {{ color:#666; font-size:14px; }}

.finding {{
  border-left:6px solid #ddd;
}}

.finding.critical {{ border-left-color:#c0392b; }}
.finding.warning  {{ border-left-color:#e67e22; }}
.finding.info     {{ border-left-color:#2980b9; }}

.impact-badge {{
  display:inline-block;
  margin-left:10px;
  padding:4px 10px;
  border-radius:999px;
  font-size:12px;
  font-weight:900;
}}

.impact-high   {{ background:#ffd6d6; }}
.impact-medium {{ background:#fff2c2; }}
.impact-low    {{ background:#dff3e6; }}

@media print {{
  body {{ background:#fff; padding:0; }}
  .actionbar {{ display:none; }}
  .card {{ box-shadow:none; break-inside:avoid; }}
}}
</style>
</head>

<body>

<div class=""card"">
  <h1>TrustFix – System Health Report</h1>
  <div class=""meta"">Generated at: {H(DateTime.Now.ToString())}</div>
</div>

<div class=""actionbar"">
  <a class=""btn"" href=""#recommended-actions"">Recommended actions</a>
  <a class=""btn btn-secondary"" href=""#"" onclick=""window.print(); return false;"">Export as PDF</a>
</div>

<div class=""card overall {overallClass}"">
  <div class=""pill {overallClass}"">Overall</div>
  <div class=""overall-title"">{H(OverallTitle(overallClass))}</div>
  <p>{H(OverallSubtitle(overallClass))}</p>
  <div class=""meta"">Worst score: <b>{worstScore}/100</b></div>

  <div class=""kpi-row"">
    <div class=""kpi"">
      <div class=""kpi-label"">Estimated improvement (Performance)</div>
      <div class=""kpi-value"">+{improvement} points</div>
      <div class=""kpi-sub"">Estimated Performance after fixes: <b>{estimatedPerfAfter}/100</b></div>
    </div>

    <div class=""kpi"">
      <div class=""kpi-label"">Snapshot highlights</div>
      <div class=""kpi-value"">{snapshot.DiskFreePercent:0.0}% disk free • {snapshot.StartupAppCount} startup apps</div>
      <div class=""kpi-sub"">RAM used: <b>{snapshot.RamUsedPercent:0.0}%</b></div>
    </div>
  </div>
</div>

<div class=""card"">
<h2>Scores</h2>
<div class=""scores"">
  <div class=""card scorebox {ScoreClass(report.Scores.Performance)}"">
    <div class=""label"">Performance</div>
    <div class=""score"">{report.Scores.Performance} / 100</div>
  </div>
  <div class=""card scorebox {ScoreClass(report.Scores.Stability)}"">
    <div class=""label"">Stability</div>
    <div class=""score"">{report.Scores.Stability} / 100</div>
  </div>
  <div class=""card scorebox {ScoreClass(report.Scores.Security)}"">
    <div class=""label"">Security</div>
    <div class=""score"">{report.Scores.Security} / 100</div>
  </div>
</div>
</div>

<div class=""card"" id=""recommended-actions"">
<h2>Recommended actions</h2>
<p class=""meta"">Estimated improvement if you apply the recommendations: <b>+{improvement} Performance</b> (to approx. <b>{estimatedPerfAfter}/100</b>).</p>
<ol>
");

        foreach (var f in report.Findings ?? [])
        {
            sb.Append($@"
<li><b>{H(f.Title)}</b><br/>{H(f.Recommendation)}</li>");
        }

        sb.Append(@"
</ol>
</div>

<div class=""card"">
<h2>Findings</h2>
");

        foreach (var f in report.Findings ?? [])
        {
            var sev = f.Severity.ToString().ToLowerInvariant();
            var impact = ImpactEstimate(f.Id);

            sb.Append($@"
<div class=""card finding {sev}"">
  <h3>
    {H(f.Title)}
    <span class=""impact-badge {impact.ClassName}"">{H(impact.Label)}</span>
  </h3>
  <p>{H(f.Description)}</p>
  <p><b>Evidence:</b> {H(f.Evidence)}</p>
  <p><b>Recommendation:</b> {H(f.Recommendation)}</p>
</div>");
        }

        sb.Append(@"
</div>

</body>
</html>
");

        return sb.ToString();
    }
}
