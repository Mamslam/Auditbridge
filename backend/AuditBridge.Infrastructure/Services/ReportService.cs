using AuditBridge.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace AuditBridge.Infrastructure.Services;

/// <summary>
/// Computes conformity scores from audit responses and generates a PDF report.
///
/// Scoring formula (per question):
///   compliant  = 100 pts
///   minor      = 75 pts    (minor non-conformity, correctable)
///   major      = 25 pts    (major non-conformity, serious gap)
///   critical   = 0 pts     (critical non-conformity, blocking)
///   na         = excluded from denominator
///   pending    = excluded from denominator
///
/// Per-question weight is based on criticality:
///   critical = 4 × weight
///   major    = 3 × weight
///   minor    = 2 × weight
///   info     = 1 × weight
///
/// Global score = Σ(weight × pts) / Σ(weight × 100) × 100
/// </summary>
public class ReportService
{
    public AuditScoreResult ComputeScore(Audit audit)
    {
        // Parse the template snapshot to get section/question structure
        var sectionMap = ParseSnapshot(audit.TemplateSnapshot);
        var responses = audit.Responses.ToDictionary(r => r.QuestionId);

        var sectionScores = new List<SectionScore>();
        int totalConform = 0, totalMinor = 0, totalMajor = 0, totalCritical = 0, totalNa = 0, totalPending = 0;
        double totalWeightedScore = 0;
        double totalWeight = 0;

        foreach (var section in sectionMap)
        {
            double sectionWeightedScore = 0;
            double sectionWeight = 0;
            int sConform = 0, sMinor = 0, sMajor = 0, sCritical = 0, sNa = 0;

            foreach (var q in section.Questions)
            {
                double questionWeight = q.Criticality switch
                {
                    "critical" => 4.0,
                    "major"    => 3.0,
                    "minor"    => 2.0,
                    _          => 1.0,  // info / observation
                };

                if (!responses.TryGetValue(q.Id, out var resp) || resp.Conformity is null or "pending")
                {
                    totalPending++;
                    continue; // excluded from score
                }

                double pts = resp.Conformity switch
                {
                    "compliant" => 100.0,
                    "minor"     => 75.0,
                    "major"     => 25.0,
                    "critical"  => 0.0,
                    "na"        => -1.0,   // sentinel for NA
                    _           => -1.0,
                };

                if (pts < 0)
                {
                    sNa++; totalNa++;
                    continue; // excluded from denominator
                }

                sectionWeightedScore += questionWeight * pts;
                sectionWeight += questionWeight * 100.0;

                switch (resp.Conformity)
                {
                    case "compliant": sConform++; totalConform++; break;
                    case "minor":     sMinor++; totalMinor++; break;
                    case "major":     sMajor++; totalMajor++; break;
                    case "critical":  sCritical++; totalCritical++; break;
                }
            }

            var sectionPct = sectionWeight > 0
                ? Math.Round(sectionWeightedScore / sectionWeight * 100, 1)
                : (double?)null;

            sectionScores.Add(new SectionScore(
                SectionId: section.Id,
                Title: section.Title,
                ConformityPct: sectionPct,
                ConformCount: sConform,
                MinorCount: sMinor,
                MajorCount: sMajor,
                CriticalCount: sCritical,
                NaCount: sNa,
                TotalQuestions: section.Questions.Count
            ));

            totalWeightedScore += sectionWeightedScore;
            totalWeight += sectionWeight;
        }

        var globalScore = totalWeight > 0
            ? Math.Round(totalWeightedScore / totalWeight * 100, 1)
            : 0.0;

        int totalAnswered = totalConform + totalMinor + totalMajor + totalCritical;
        int totalQuestions = totalAnswered + totalNa + totalPending;

        return new AuditScoreResult(
            GlobalScore: (decimal)globalScore,
            TotalQuestions: totalQuestions,
            ConformCount: totalConform,
            MinorCount: totalMinor,
            MajorCount: totalMajor,
            CriticalCount: totalCritical,
            NaCount: totalNa,
            PendingCount: totalPending,
            SectionScores: sectionScores
        );
    }

    public byte[] GeneratePdf(Audit audit, AuditScoreResult score, IEnumerable<AuditFinding> findings)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(ComposeHeader(audit));
                page.Content().Element(ComposeContent(audit, score, findings));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("AuditBridge · Rapport généré le ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(DateTimeOffset.UtcNow.ToString("dd/MM/yyyy HH:mm") + " UTC").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(" · Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span("/").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ── PDF composition helpers ────────────────────────────────────────────

    private static Action<IContainer> ComposeHeader(Audit audit)
        => container =>
        {
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(12).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("RAPPORT D'AUDIT").FontSize(18).Bold().FontColor("#1E3A5F");
                    col.Item().Text(audit.Title).FontSize(13).SemiBold().FontColor(Colors.Grey.Darken3);
                    col.Item().PaddingTop(4).Text(text =>
                    {
                        text.Span("Référentiel : ").Bold();
                        text.Span(audit.Referential?.Name ?? "—");
                        if (audit.Referential?.Version is not null)
                            text.Span($" ({audit.Referential.Version})").FontColor(Colors.Grey.Darken1);
                    });
                    if (audit.ClientOrgName is not null)
                        col.Item().Text(text =>
                        {
                            text.Span("Organisation auditée : ").Bold();
                            text.Span(audit.ClientOrgName);
                        });
                    col.Item().Text(text =>
                    {
                        text.Span("Statut : ").Bold();
                        text.Span(audit.Status.ToUpperInvariant()).FontColor(
                            audit.Status == "completed" ? Colors.Green.Darken2 : Colors.Orange.Darken2);
                    });
                });
                row.ConstantItem(140).AlignRight().AlignMiddle().Column(col =>
                {
                    if (audit.DueDate.HasValue)
                        col.Item().AlignRight().Text(text =>
                        {
                            text.Span("Échéance : ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            text.Span(audit.DueDate.Value.ToString("dd/MM/yyyy")).FontSize(9).Bold();
                        });
                    col.Item().AlignRight().PaddingTop(4).Text($"#{audit.Id.ToString()[..8].ToUpper()}")
                        .FontSize(8).FontColor(Colors.Grey.Lighten1).FontFamily("Courier New");
                });
            });
        };

    private static Action<IContainer> ComposeContent(Audit audit, AuditScoreResult score, IEnumerable<AuditFinding> findings)
        => container =>
        {
            container.Column(col =>
            {
                // ── Executive Summary ──────────────────────────────────────
                col.Item().PaddingTop(20).Text("1. SYNTHÈSE EXÉCUTIVE").FontSize(13).Bold().FontColor("#1E3A5F");
                col.Item().PaddingTop(8).Row(row =>
                {
                    // Global score gauge
                    row.ConstantItem(160).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12)
                        .AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("Score Global").FontSize(9).FontColor(Colors.Grey.Darken1);
                            var scoreColor = score.GlobalScore >= 80 ? Colors.Green.Darken2
                                           : score.GlobalScore >= 60 ? Colors.Orange.Darken1
                                           : Colors.Red.Darken2;
                            c.Item().AlignCenter().Text($"{score.GlobalScore:F1}%")
                                .FontSize(28).Bold().FontColor(scoreColor);
                            c.Item().AlignCenter().PaddingTop(4).Text(
                                score.GlobalScore >= 80 ? "✓ Conforme" :
                                score.GlobalScore >= 60 ? "⚠ À améliorer" : "✗ Non conforme")
                                .FontSize(9).FontColor(scoreColor);
                        });

                    row.RelativeItem().PaddingLeft(16).Column(c =>
                    {
                        c.Item().Text("Répartition des réponses").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                        c.Item().PaddingTop(6).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(50); cd.ConstantColumn(60); });
                            static IContainer CellStyle(IContainer x) => x.PaddingVertical(3).PaddingHorizontal(6);
                            t.Cell().Element(CellStyle).Text("Conformes").FontSize(9);
                            t.Cell().Element(CellStyle).AlignRight().Text(score.ConformCount.ToString()).FontSize(9).FontColor(Colors.Green.Darken2).Bold();
                            t.Cell().Element(CellStyle).AlignRight().Text($"{Pct(score.ConformCount, score.TotalAnswered)}%").FontSize(9).FontColor(Colors.Grey.Darken1);

                            t.Cell().Element(CellStyle).Text("NC Mineurs").FontSize(9);
                            t.Cell().Element(CellStyle).AlignRight().Text(score.MinorCount.ToString()).FontSize(9).FontColor(Colors.Orange.Darken1).Bold();
                            t.Cell().Element(CellStyle).AlignRight().Text($"{Pct(score.MinorCount, score.TotalAnswered)}%").FontSize(9).FontColor(Colors.Grey.Darken1);

                            t.Cell().Element(CellStyle).Text("NC Majeurs").FontSize(9);
                            t.Cell().Element(CellStyle).AlignRight().Text(score.MajorCount.ToString()).FontSize(9).FontColor("#E65100").Bold();
                            t.Cell().Element(CellStyle).AlignRight().Text($"{Pct(score.MajorCount, score.TotalAnswered)}%").FontSize(9).FontColor(Colors.Grey.Darken1);

                            t.Cell().Element(CellStyle).Text("NC Critiques").FontSize(9);
                            t.Cell().Element(CellStyle).AlignRight().Text(score.CriticalCount.ToString()).FontSize(9).FontColor(Colors.Red.Darken2).Bold();
                            t.Cell().Element(CellStyle).AlignRight().Text($"{Pct(score.CriticalCount, score.TotalAnswered)}%").FontSize(9).FontColor(Colors.Grey.Darken1);

                            t.Cell().Element(CellStyle).Text("N/A").FontSize(9).FontColor(Colors.Grey.Darken1);
                            t.Cell().Element(CellStyle).AlignRight().Text(score.NaCount.ToString()).FontSize(9).FontColor(Colors.Grey.Darken1);
                            t.Cell().Element(CellStyle).AlignRight().Text("—").FontSize(9).FontColor(Colors.Grey.Lighten1);

                            t.Cell().Element(CellStyle).Text("En attente").FontSize(9).FontColor(Colors.Grey.Darken1);
                            t.Cell().Element(CellStyle).AlignRight().Text(score.PendingCount.ToString()).FontSize(9).FontColor(Colors.Grey.Darken1);
                            t.Cell().Element(CellStyle).AlignRight().Text("—").FontSize(9).FontColor(Colors.Grey.Lighten1);
                        });
                    });
                });

                // ── Section Scores ─────────────────────────────────────────
                if (score.SectionScores.Any())
                {
                    col.Item().PaddingTop(24).Text("2. SCORES PAR SECTION").FontSize(13).Bold().FontColor("#1E3A5F");
                    col.Item().PaddingTop(8).Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(4);
                            cd.ConstantColumn(65);  // score
                            cd.ConstantColumn(55);  // conform
                            cd.ConstantColumn(55);  // minor
                            cd.ConstantColumn(55);  // major
                            cd.ConstantColumn(55);  // critical
                        });

                        static IContainer HeaderCell(IContainer x) =>
                            x.Background("#1E3A5F").Padding(6).AlignCenter();
                        static IContainer BodyCell(IContainer x) =>
                            x.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5);

                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Section").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Score").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Conf.").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Mineur").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Majeur").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Critique").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        foreach (var s in score.SectionScores)
                        {
                            var scoreStr = s.ConformityPct.HasValue ? $"{s.ConformityPct:F1}%" : "—";
                            var scoreColor = s.ConformityPct >= 80 ? Colors.Green.Darken2
                                           : s.ConformityPct >= 60 ? Colors.Orange.Darken1
                                           : s.ConformityPct.HasValue ? Colors.Red.Darken2 : Colors.Grey.Darken1;
                            t.Cell().Element(BodyCell).Text(s.Title).FontSize(9);
                            t.Cell().Element(BodyCell).AlignCenter().Text(scoreStr).FontSize(9).Bold().FontColor(scoreColor);
                            t.Cell().Element(BodyCell).AlignCenter().Text(s.ConformCount.ToString()).FontSize(9).FontColor(Colors.Green.Darken2);
                            t.Cell().Element(BodyCell).AlignCenter().Text(s.MinorCount.ToString()).FontSize(9).FontColor(Colors.Orange.Darken1);
                            t.Cell().Element(BodyCell).AlignCenter().Text(s.MajorCount.ToString()).FontSize(9).FontColor("#E65100");
                            t.Cell().Element(BodyCell).AlignCenter().Text(s.CriticalCount.ToString()).FontSize(9).FontColor(Colors.Red.Darken2);
                        }
                    });
                }

                // ── Findings ──────────────────────────────────────────────
                var findingList = findings.ToList();
                if (findingList.Count > 0)
                {
                    col.Item().PaddingTop(24).Text("3. CONSTATS").FontSize(13).Bold().FontColor("#1E3A5F");
                    col.Item().PaddingTop(4).Text($"{findingList.Count} constat(s) documenté(s).")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);

                    int findingIdx = 1;
                    foreach (var f in findingList.OrderBy(f => f.FindingType).ThenBy(f => f.CreatedAt))
                    {
                        (string typeLabel, string typeColor) = f.FindingType switch
                        {
                            "nc_critical"  => ("NC CRITIQUE", (string)Colors.Red.Darken2),
                            "nc_major"     => ("NC MAJEUR",   "#E65100"),
                            "nc_minor"     => ("NC MINEUR",   (string)Colors.Orange.Darken1),
                            "observation"  => ("OBSERVATION", (string)Colors.Blue.Darken1),
                            _              => ("OFI",         (string)Colors.Teal.Darken1),
                        };

                        col.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Column(fc =>
                        {
                            fc.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(r =>
                            {
                                r.AutoItem().Text($"#{findingIdx:D2}  ").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                                r.AutoItem().Border(1).BorderColor(typeColor).Background(typeColor).Padding(2).PaddingHorizontal(5)
                                    .Text(typeLabel).FontSize(8).Bold().FontColor(Colors.White);
                                r.RelativeItem().PaddingLeft(8).Text(f.Title).FontSize(10).Bold();
                            });
                            fc.Item().Padding(10).Column(dc =>
                            {
                                if (!string.IsNullOrEmpty(f.Description))
                                {
                                    dc.Item().Text("Description").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                                    dc.Item().PaddingTop(2).Text(f.Description).FontSize(9);
                                }
                                if (!string.IsNullOrEmpty(f.ObservedEvidence))
                                {
                                    dc.Item().PaddingTop(6).Text("Evidence observée").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                                    dc.Item().PaddingTop(2).Text(f.ObservedEvidence).FontSize(9).FontColor(Colors.Grey.Darken3).Italic();
                                }
                                if (!string.IsNullOrEmpty(f.RegulatoryRef))
                                {
                                    dc.Item().PaddingTop(6).Text(text =>
                                    {
                                        text.Span("Référence : ").FontSize(8).Bold();
                                        text.Span(f.RegulatoryRef).FontSize(8).FontColor(Colors.Blue.Darken2);
                                    });
                                }
                                if (f.Capas.Any())
                                {
                                    dc.Item().PaddingTop(6).Text($"CAPAs associées : {f.Capas.Count}")
                                        .FontSize(8).FontColor(Colors.Grey.Darken2);
                                }
                            });
                        });
                        findingIdx++;
                    }
                }

                // ── CAPA Action Plan ───────────────────────────────────────
                var allCapas = audit.Capas.Where(c => c.Status != "cancelled").ToList();
                if (allCapas.Count > 0)
                {
                    col.Item().PaddingTop(24).Text("4. PLAN D'ACTIONS CORRECTIVES (CAPA)").FontSize(13).Bold().FontColor("#1E3A5F");
                    col.Item().PaddingTop(8).Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(24);   // #
                            cd.RelativeColumn(3);    // title
                            cd.ConstantColumn(55);   // type
                            cd.ConstantColumn(55);   // priority
                            cd.RelativeColumn(2);    // assignee
                            cd.ConstantColumn(65);   // due date
                            cd.ConstantColumn(70);   // status
                        });

                        static IContainer HeaderCell(IContainer x) =>
                            x.Background("#1E3A5F").Padding(6);
                        static IContainer BodyCell(IContainer x) =>
                            x.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5);

                        t.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("#").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Action").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Type").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Priorité").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Responsable").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Échéance").FontSize(8).Bold().FontColor(Colors.White);
                            h.Cell().Element(HeaderCell).Text("Statut").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        int idx = 1;
                        foreach (var c in allCapas.OrderBy(c => c.Priority == "critical" ? 0 : c.Priority == "high" ? 1 : 2))
                        {
                            var statusColor = c.Status switch
                            {
                                "verified"             => Colors.Green.Darken2,
                                "pending_verification" => Colors.Teal.Darken1,
                                "in_progress"          => Colors.Blue.Darken1,
                                _                      => Colors.Red.Darken1,
                            };
                            t.Cell().Element(BodyCell).Text(idx.ToString()).FontSize(8).FontColor(Colors.Grey.Darken1);
                            t.Cell().Element(BodyCell).Column(cc =>
                            {
                                cc.Item().Text(c.Title).FontSize(9).SemiBold();
                                if (!string.IsNullOrEmpty(c.Description))
                                    cc.Item().Text(c.Description).FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                            t.Cell().Element(BodyCell).Text(c.ActionType).FontSize(8);
                            t.Cell().Element(BodyCell).Text(c.Priority.ToUpperInvariant()).FontSize(8).Bold()
                                .FontColor(c.Priority == "critical" ? Colors.Red.Darken2 : c.Priority == "high" ? Colors.Orange.Darken1 : Colors.Grey.Darken2);
                            t.Cell().Element(BodyCell).Text(c.AssignedToEmail ?? "—").FontSize(8).FontColor(Colors.Grey.Darken2);
                            t.Cell().Element(BodyCell).Text(c.DueDate?.ToString("dd/MM/yyyy") ?? "—").FontSize(8);
                            t.Cell().Element(BodyCell).Text(c.Status.Replace("_", " ").ToUpperInvariant()).FontSize(8).Bold().FontColor(statusColor);
                            idx++;
                        }
                    });
                }
            });
        };

    private static int Pct(int value, int total) =>
        total > 0 ? (int)Math.Round(value * 100.0 / total) : 0;

    // ── Snapshot parser ───────────────────────────────────────────────────

    private static List<SectionSnapshot> ParseSnapshot(string snapshotJson)
    {
        var result = new List<SectionSnapshot>();
        try
        {
            var doc = JsonDocument.Parse(snapshotJson);
            JsonElement sectionsEl;
            if (!doc.RootElement.TryGetProperty("Sections", out sectionsEl) &&
                !doc.RootElement.TryGetProperty("sections", out sectionsEl))
                return result;

            foreach (var s in sectionsEl.EnumerateArray())
            {
                var sId = GetGuid(s, "Id", "id");
                var sTitle = GetStr(s, "Title", "title") ?? "Section";
                var questions = new List<QuestionSnapshot>();

                JsonElement qsEl;
                if (s.TryGetProperty("Questions", out qsEl) || s.TryGetProperty("questions", out qsEl))
                {
                    foreach (var q in qsEl.EnumerateArray())
                    {
                        var qId = GetGuid(q, "Id", "id");
                        if (qId == Guid.Empty) continue;
                        var criticality = GetStr(q, "Criticality", "criticality") ?? "major";
                        questions.Add(new QuestionSnapshot(qId, criticality));
                    }
                }

                if (sId != Guid.Empty)
                    result.Add(new SectionSnapshot(sId, sTitle, questions));
            }
        }
        catch { /* snapshot parse failure — return empty */ }
        return result;
    }

    private static Guid GetGuid(JsonElement el, string key1, string key2)
    {
        if (el.TryGetProperty(key1, out var v) || el.TryGetProperty(key2, out v))
            return v.TryGetGuid(out var g) ? g : Guid.Empty;
        return Guid.Empty;
    }

    private static string? GetStr(JsonElement el, string key1, string key2)
    {
        if (el.TryGetProperty(key1, out var v) || el.TryGetProperty(key2, out v))
            return v.GetString();
        return null;
    }

    private record SectionSnapshot(Guid Id, string Title, List<QuestionSnapshot> Questions);
    private record QuestionSnapshot(Guid Id, string Criticality);
}

// ── Value objects returned from scoring ───────────────────────────────────

public record AuditScoreResult(
    decimal GlobalScore,
    int TotalQuestions,
    int ConformCount,
    int MinorCount,
    int MajorCount,
    int CriticalCount,
    int NaCount,
    int PendingCount,
    IReadOnlyList<SectionScore> SectionScores)
{
    public int TotalAnswered => ConformCount + MinorCount + MajorCount + CriticalCount;
    public int TotalNonConform => MinorCount + MajorCount + CriticalCount;
}

public record SectionScore(
    Guid SectionId,
    string Title,
    double? ConformityPct,
    int ConformCount,
    int MinorCount,
    int MajorCount,
    int CriticalCount,
    int NaCount,
    int TotalQuestions
);
