namespace AuditBridge.Application.DTOs;

public record OverdueAuditItem(
    Guid Id,
    string Title,
    string Status,
    DateOnly DueDate,
    int DaysOverdue,
    string? ReferentialCode
);

public record CapaAgingItem(
    Guid Id,
    string Title,
    string Priority,
    string Status,
    DateOnly? DueDate,
    int? DaysOverdue,
    string AuditTitle
);

public record CapaAgingSummary(
    int Total,
    int Overdue,
    int Critical,
    int High,
    int Medium,
    int Low,
    List<CapaAgingItem> OverdueItems
);

public record FindingDistribution(
    int NcCritical,
    int NcMajor,
    int NcMinor,
    int Observation,
    int Ofi
);

public record MonthlyScorePoint(string Month, double AvgScore, int Count);

public record RepeatFinding(string Title, int Count, List<string> AuditTitles);

public record DashboardDto(
    // Audit counts
    int TotalAudits,
    int Active,
    int Submitted,
    int Completed,
    int Overdue,
    double? AvgConformityScore,

    // Detail lists
    List<OverdueAuditItem> OverdueAudits,
    CapaAgingSummary CapaAging,
    FindingDistribution FindingDistribution,
    List<MonthlyScorePoint> ConformityTrend,
    List<RepeatFinding> RepeatFindings
);
