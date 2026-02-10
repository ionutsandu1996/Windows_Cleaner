namespace TrustFix.Core.Domain;
public sealed record Finding(
    string Id,
    string Title,
    string Description,
    Severity Severity,
    string Evidence,
    string Recommendation,
    string Impact = "Medium",
    long? ReclaimableBytes = null

);