using TrustFix.Collectors.Mock;
using TrustFix.Engine;
using TrustFix.Reporting;
using System.IO;

var scenario = args.FirstOrDefault()?.ToLowerInvariant() ?? "slow";

var snapshot = scenario switch
{
    "ram" => MockScenarios.RamPressure(),
    _ => MockScenarios.SlowLaptop()
};

var engine = new TrustFixEngine();
var report = engine.Evaluate(snapshot);

Console.WriteLine($"Scores: Performance={report.Scores.Performance}, Stability={report.Scores.Stability}, Security={report.Scores.Security}");
Console.WriteLine();

foreach (var f in report.Findings)
{
    Console.WriteLine($"[{f.Severity}] {f.Title}");
    Console.WriteLine($"- {f.Evidence}");
    Console.WriteLine($"- Recommendation: {f.Recommendation}");
    Console.WriteLine();
}

var html = HtmlReportGenerator.Generate(report, snapshot);


var outputPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    $"trustfix-report-{scenario}.html"
);

File.WriteAllText(outputPath, html);

Console.WriteLine($"HTML report generated: {outputPath}");
