using AuditBridge.Domain.Entities;
using AuditBridge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AuditBridge.Infrastructure.Seeds;

public class ReferentialSeeder(AppDbContext db)
{
    private static readonly (string Slug, string Label, string Color, string Icon)[] Categories =
    [
        ("pharma",      "Pharmaceutique",   "#3B82F6", "💊"),
        ("qualite",     "Qualité",          "#10B981", "✅"),
        ("cyber",       "Cybersécurité",    "#8B5CF6", "🔒"),
        ("rgpd",        "Données",          "#F59E0B", "🛡️"),
        ("alimentaire", "Agroalimentaire",  "#EF4444", "🌾"),
        ("rse",         "RSE / ESG",        "#14B8A6", "🌱"),
        ("finance",     "Finance / IT",     "#6366F1", "🏦"),
        ("custom",      "Personnalisé",     "#6B7280", "⚙️"),
    ];

    private static readonly (string Code, string Name, string Category, string? Version)[] SystemReferentials =
    [
        ("EU_GMP",    "EU GMP (Bonnes Pratiques de Fabrication)",         "pharma",      "2023"),
        ("ISO_9001",  "ISO 9001:2015 — Management de la qualité",         "qualite",     "2015"),
        ("ISO_27001", "ISO/IEC 27001:2022 — Sécurité de l'information",   "cyber",       "2022"),
        ("ISO_14001", "ISO 14001:2015 — Management environnemental",      "qualite",     "2015"),
        ("ISO_45001", "ISO 45001:2018 — Santé et sécurité au travail",    "qualite",     "2018"),
        ("ISO_13485", "ISO 13485:2016 — Dispositifs médicaux",            "pharma",      "2016"),
        ("RGPD",      "RGPD — Règlement Général Protection des Données",  "rgpd",        "2018"),
        ("HACCP",     "HACCP — Hazard Analysis Critical Control Points",  "alimentaire", null),
        ("IFS_FOOD",  "IFS Food v8",                                      "alimentaire", "v8"),
        ("BRC_FOOD",  "BRCGS Food Safety Issue 9",                        "alimentaire", "9"),
        ("ISO_22000", "ISO 22000:2018 — Sécurité des aliments",           "alimentaire", "2018"),
        ("CSRD",      "CSRD — Corporate Sustainability Reporting",        "rse",         "2024"),
        ("NIS2",      "NIS2 — Directive Cybersécurité EU 2022/2555",      "cyber",       "2022"),
        ("DORA",      "DORA — Digital Operational Resilience Act",        "finance",     "2025"),
        ("HDS",       "HDS — Hébergeurs de Données de Santé",             "pharma",      "2023"),
        ("GDP",       "GDP — Good Distribution Practice",                  "pharma",      "2013"),
        ("GLP",       "GLP — Good Laboratory Practice",                   "pharma",      "2016"),
        ("SOC2",      "SOC 2 — Service Organization Controls",            "cyber",       null),
    ];

    // Template JSON files that exist
    private static readonly string[] SeededCodes =
    [
        "EU_GMP", "ISO_9001", "ISO_27001", "RGPD", "HACCP",
        "ISO_14001", "ISO_45001", "ISO_13485",
        "IFS_FOOD", "BRC_FOOD", "ISO_22000",
        "CSRD", "NIS2", "DORA", "HDS", "GDP", "GLP", "SOC2",
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var seedDir = GetSeedDir();

        if (!await db.ReferentialCategories.AnyAsync(ct))
        {
            // 1. Seed categories
            var categoryMap = new Dictionary<string, ReferentialCategory>();
            foreach (var (slug, label, color, icon) in Categories)
            {
                var cat = ReferentialCategory.Create(slug, label, color, icon);
                db.ReferentialCategories.Add(cat);
                categoryMap[slug] = cat;
            }
            await db.SaveChangesAsync(ct);

            // 2. Seed referentials
            foreach (var (code, name, categorySlug, version) in SystemReferentials)
            {
                var categoryId = categoryMap.TryGetValue(categorySlug, out var cat) ? cat.Id : (Guid?)null;
                db.Referentials.Add(Referential.CreateSystem(code, name, categoryId, version));
            }
            await db.SaveChangesAsync(ct);
        }

        // 3. Load questions for any referential that has no sections yet
        var refsWithoutSections = await db.Referentials
            .Where(r => r.IsSystem && !db.TemplateSections.Any(s => s.ReferentialId == r.Id))
            .ToDictionaryAsync(r => r.Code, ct);

        foreach (var code in SeededCodes)
        {
            if (!refsWithoutSections.TryGetValue(code, out var referential)) continue;
            var filePath = Path.Combine(seedDir, $"{code.ToLower()}.json");
            if (!File.Exists(filePath)) continue;
            await LoadQuestionsFromJsonAsync(filePath, referential.Id, ct);
        }
    }

    private static string GetSeedDir()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Seeds", "Templates");
        if (!Directory.Exists(dir))
            dir = Path.Combine(
                Path.GetDirectoryName(typeof(ReferentialSeeder).Assembly.Location)!,
                "..", "..", "..", "Seeds", "Templates");
        return dir;
    }

    private async Task LoadQuestionsFromJsonAsync(
        string filePath, Guid referentialId, CancellationToken ct)
    {
        using var fs = File.OpenRead(filePath);
        using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: ct);
        var root = doc.RootElement;

        if (!root.TryGetProperty("sections", out var sections)) return;

        int sectionOrder = 0;
        foreach (var sectionEl in sections.EnumerateArray())
        {
            var sectionCode = sectionEl.TryGetProperty("code", out var c) ? c.GetString() : null;
            var sectionTitle = sectionEl.GetProperty("title").GetString()!;
            var section = TemplateSection.Create(referentialId, sectionTitle, sectionOrder++, sectionCode);
            db.TemplateSections.Add(section);
            await db.SaveChangesAsync(ct);  // persist to get the section Id

            if (!sectionEl.TryGetProperty("questions", out var questions)) continue;

            int qOrder = 0;
            foreach (var qEl in questions.EnumerateArray())
            {
                var question = TemplateQuestion.Create(
                    referentialId: referentialId,
                    question: qEl.GetProperty("question").GetString()!,
                    answerType: qEl.TryGetProperty("answer_type", out var at) ? at.GetString()! : "text",
                    criticality: qEl.TryGetProperty("criticality", out var cr) ? cr.GetString()! : "major",
                    orderIndex: qOrder++,
                    code: qEl.TryGetProperty("code", out var qc) ? qc.GetString() : null,
                    sectionId: section.Id,
                    guidance: qEl.TryGetProperty("guidance", out var g) ? g.GetString() : null,
                    isMandatory: !qEl.TryGetProperty("is_mandatory", out var im) || im.GetBoolean(),
                    expectedEvidence: qEl.TryGetProperty("expected_evidence", out var ev)
                        ? ev.EnumerateArray().Select(e => e.GetString()!).ToArray()
                        : null);

                db.TemplateQuestions.Add(question);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
