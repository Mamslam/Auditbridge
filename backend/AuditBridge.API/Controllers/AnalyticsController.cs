using AuditBridge.Application.DTOs;
using AuditBridge.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditBridge.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContext) : ControllerBase
{
    private Guid? CurrentOrgId => httpContext.HttpContext?.Items["CurrentOrgId"] as Guid?;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        if (CurrentOrgId is null) return Unauthorized();
        var orgId = CurrentOrgId.Value;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Load all data in parallel
        var auditsTask = unitOfWork.Audits.GetByOrgAsync(orgId, ct);
        var capasTask = unitOfWork.Audits.GetOpenCapasByOrgAsync(orgId, ct);
        var reportsTask = unitOfWork.Audits.GetReportsByOrgAsync(orgId, ct);
        var findingsTask = unitOfWork.Audits.GetAllFindingsByOrgAsync(orgId, ct);

        await Task.WhenAll(auditsTask, capasTask, reportsTask, findingsTask);

        var audits = (await auditsTask).ToList();
        var capas = (await capasTask).ToList();
        var reports = (await reportsTask).ToList();
        var findings = (await findingsTask).ToList();

        // Build lookup maps
        var auditMap = audits.ToDictionary(a => a.Id);

        // ── Audit counts ──────────────────────────────────────────────────
        var active = audits.Count(a => a.Status == "active");
        var submitted = audits.Count(a => a.Status == "submitted");
        var completed = audits.Count(a => a.Status == "completed");

        var overdueAudits = audits
            .Where(a => a.DueDate.HasValue
                && a.DueDate.Value < today
                && a.Status is "draft" or "active" or "submitted")
            .Select(a => new OverdueAuditItem(
                a.Id, a.Title, a.Status, a.DueDate!.Value,
                (today.ToDateTime(TimeOnly.MinValue) - a.DueDate.Value.ToDateTime(TimeOnly.MinValue)).Days,
                a.Referential?.Code))
            .OrderBy(a => a.DaysOverdue)
            .ToList();

        // ── Avg score ─────────────────────────────────────────────────────
        var scores = reports
            .Where(r => r.ConformityScore.HasValue)
            .Select(r => (double)r.ConformityScore!.Value)
            .ToList();
        double? avgScore = scores.Count > 0 ? Math.Round(scores.Average(), 1) : null;

        // ── CAPA aging ────────────────────────────────────────────────────
        var overdueCAPAs = capas
            .Where(c => c.DueDate.HasValue && c.DueDate.Value < today)
            .Select(c => new CapaAgingItem(
                c.Id, c.Title, c.Priority, c.Status, c.DueDate,
                c.DueDate.HasValue
                    ? (today.ToDateTime(TimeOnly.MinValue) - c.DueDate.Value.ToDateTime(TimeOnly.MinValue)).Days
                    : null,
                auditMap.TryGetValue(c.AuditId, out var a) ? a.Title : c.AuditId.ToString()))
            .OrderByDescending(c => c.DaysOverdue)
            .Take(10)
            .ToList();

        var capaAging = new CapaAgingSummary(
            Total: capas.Count,
            Overdue: overdueCAPAs.Count,
            Critical: capas.Count(c => c.Priority == "critical"),
            High: capas.Count(c => c.Priority == "high"),
            Medium: capas.Count(c => c.Priority == "medium"),
            Low: capas.Count(c => c.Priority == "low"),
            OverdueItems: overdueCAPAs
        );

        // ── Finding distribution ───────────────────────────────────────────
        var dist = new FindingDistribution(
            NcCritical: findings.Count(f => f.FindingType == "nc_critical"),
            NcMajor: findings.Count(f => f.FindingType == "nc_major"),
            NcMinor: findings.Count(f => f.FindingType == "nc_minor"),
            Observation: findings.Count(f => f.FindingType == "observation"),
            Ofi: findings.Count(f => f.FindingType == "ofi")
        );

        // ── Conformity trend (last 6 months) ──────────────────────────────
        var trend = reports
            .Where(r => r.ConformityScore.HasValue && r.GeneratedAt >= DateTimeOffset.UtcNow.AddMonths(-6))
            .GroupBy(r => new { r.GeneratedAt.Year, r.GeneratedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyScorePoint(
                Month: $"{g.Key.Year}-{g.Key.Month:00}",
                AvgScore: Math.Round(g.Average(r => (double)r.ConformityScore!.Value), 1),
                Count: g.Count()
            ))
            .ToList();

        // ── Repeat findings (same title, >1 audit) ────────────────────────
        var repeatFindings = findings
            .GroupBy(f => f.Title.Trim().ToLowerInvariant())
            .Where(g => g.Select(f => f.AuditId).Distinct().Count() > 1)
            .Select(g => new RepeatFinding(
                Title: g.First().Title,
                Count: g.Select(f => f.AuditId).Distinct().Count(),
                AuditTitles: g.Select(f => auditMap.TryGetValue(f.AuditId, out var a) ? a.Title : "")
                    .Where(t => t.Length > 0).Distinct().Take(4).ToList()
            ))
            .OrderByDescending(r => r.Count)
            .Take(5)
            .ToList();

        return Ok(new DashboardDto(
            TotalAudits: audits.Count,
            Active: active,
            Submitted: submitted,
            Completed: completed,
            Overdue: overdueAudits.Count,
            AvgConformityScore: avgScore,
            OverdueAudits: overdueAudits,
            CapaAging: capaAging,
            FindingDistribution: dist,
            ConformityTrend: trend,
            RepeatFindings: repeatFindings
        ));
    }
}
