using TrustFix.Core.Domain;

namespace TrustFix.Engine;

public sealed record EvaluationReport(ScoreSummary Scores, IReadOnlyList<Finding> Findings);
