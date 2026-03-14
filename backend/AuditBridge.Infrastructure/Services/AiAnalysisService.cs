using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuditBridge.Infrastructure.Services;

public record ClassificationResult(
    string Conformity,
    string CriticalityLevel,
    double Confidence,
    string? ClauseReference,
    string[] Gaps,
    string[] MissingEvidence,
    string AuditorRecommendation,
    string RegulatoryRisk
);

public record CriticalGap(string QuestionCode, string Issue, string SuggestedFix);

public record PreventiveAnalysis(
    string OverallReadiness,
    int ReadinessScore,
    List<CriticalGap> CriticalGaps,
    string[] MissingDocuments,
    string[] Strengths,
    string GlobalRecommendation
);

public record ReportNarrative(
    string ExecutiveSummary,
    string AuditContext,
    string KeyFindings,
    string RiskAssessment,
    string CapaNarrative,
    string Conclusion,
    string? CertificationRecommendation
);

public record FindingSummary(
    string FindingType,
    string Title,
    string Description,
    string? ObservedEvidence,
    string? RegulatoryRef,
    string? Recommendation
);

public record CapaSuggestion(
    string Title,
    string Description,
    string RootCause,
    string ActionType,
    string Priority,
    string? Rationale
);

public record AuditAnswer(
    string Answer,
    string[]? References,
    string? Confidence,
    string? Disclaimer
);

public record QuestionAnswerPair(
    string QuestionCode, string QuestionText,
    string? AnswerValue, string? AnswerNotes,
    string[] UploadedDocuments, string Criticality);

public class AiAnalysisService(IConfiguration config, ILogger<AiAnalysisService> logger)
{
    private AnthropicClient CreateClient()
    {
        var apiKey = config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey not configured.");
        return new AnthropicClient(apiKey);
    }

    // 5.1 — Classificateur universel
    public async Task<ClassificationResult?> ClassifyAsync(
        string referentialCode, string referentialName,
        string questionCode, string questionText,
        string clientAnswer, string[] uploadedDocuments,
        CancellationToken ct = default)
    {
        var docs = uploadedDocuments.Length > 0 ? string.Join(", ", uploadedDocuments) : "(aucun)";
        var prompt = $"Tu es un expert auditeur du référentiel {referentialName} ({referentialCode}).\n\n" +
            $"QUESTION D'AUDIT (code: {questionCode}):\n{questionText}\n\n" +
            $"RÉPONSE FOURNIE PAR LE CLIENT:\n{clientAnswer}\n\n" +
            $"DOCUMENTS UPLOADÉS: {docs}\n\n" +
            "Analyse cette réponse et fournis UNIQUEMENT ce JSON sans autre texte:\n" +
            "{\n  \"conformity\": \"conform|non_conform|partial|na|insufficient\",\n" +
            "  \"criticality_level\": \"critical|major|minor|observation\",\n" +
            "  \"confidence\": 0.0,\n" +
            "  \"clause_reference\": \"article ou clause exacte si applicable\",\n" +
            "  \"gaps\": [],\n" +
            "  \"missing_evidence\": [],\n" +
            "  \"auditor_recommendation\": \"recommandation courte et actionnable\",\n" +
            "  \"regulatory_risk\": \"high|medium|low|none\"\n}";

        return await CallClaudeJsonAsync<ClassificationResult>(prompt, ct);
    }

    // 5.2 — Analyse préventive avant soumission
    public async Task<PreventiveAnalysis?> AnalyzeBeforeSubmissionAsync(
        string referentialName,
        List<QuestionAnswerPair> responses,
        CancellationToken ct = default)
    {
        var responsesJson = JsonSerializer.Serialize(responses, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var prompt = $"Tu es un consultant expert en {referentialName}.\n\n" +
            "Un client s'apprête à soumettre ses réponses à un audit.\n" +
            $"Voici l'ensemble de ses réponses et preuves fournies :\n\n{responsesJson}\n\n" +
            "MISSION : Identifie les risques AVANT la soumission pour que le client puisse se corriger.\n\n" +
            "Réponds UNIQUEMENT avec ce JSON:\n" +
            "{\n  \"overall_readiness\": \"ready|needs_attention|not_ready\",\n" +
            "  \"readiness_score\": 0,\n" +
            "  \"critical_gaps\": [],\n" +
            "  \"missing_documents\": [],\n" +
            "  \"strengths\": [],\n" +
            "  \"global_recommendation\": \"message d'ensemble pour le client\"\n}";

        return await CallClaudeJsonAsync<PreventiveAnalysis>(prompt, ct);
    }

    // 5.3 — Générateur de rapport universel
    public async Task<ReportNarrative?> GenerateReportAsync(
        string referentialCode, string referentialName,
        string clientOrgName, string scope,
        decimal conformityScore, int criticalNc, int majorNc, int minorNc,
        object nonConformities, object capas,
        CancellationToken ct = default)
    {
        var ncJson = JsonSerializer.Serialize(nonConformities);
        var capaJson = JsonSerializer.Serialize(capas);

        var prompt = $"Tu es un auditeur senior expert en {referentialName}.\n\n" +
            $"Voici les données d'un audit {referentialCode} réalisé chez {clientOrgName}.\n" +
            $"Périmètre : {scope}\n\n" +
            $"DONNÉES: Score de conformité : {conformityScore}%, " +
            $"NC critiques : {criticalNc}, majeures : {majorNc}, mineures : {minorNc}\n\n" +
            $"NON-CONFORMITÉS:\n{ncJson}\n\n" +
            $"PLAN CAPA:\n{capaJson}\n\n" +
            "Génère les éléments narratifs du rapport. Réponds UNIQUEMENT avec ce JSON:\n" +
            "{\n  \"executive_summary\": \"Résumé exécutif 3-4 paragraphes\",\n" +
            "  \"audit_context\": \"Contexte et méthodologie\",\n" +
            "  \"key_findings\": \"Principaux constats\",\n" +
            "  \"risk_assessment\": \"Évaluation des risques réglementaires\",\n" +
            "  \"capa_narrative\": \"Plan d'actions correctives et priorités\",\n" +
            "  \"conclusion\": \"Conclusion et recommandations\",\n" +
            "  \"certification_recommendation\": \"Aptitude à la certification si applicable\"\n}";

        return await CallClaudeJsonAsync<ReportNarrative>(prompt, ct);
    }

    // 5.4 — Finding summarizer
    public async Task<FindingSummary?> SummarizeFindingAsync(
        string referentialCode, string referentialName,
        string rawNotes,
        CancellationToken ct = default)
    {
        var prompt = $"Tu es un auditeur expert en {referentialName} ({referentialCode}).\n\n" +
            "Un auditeur a pris les notes brutes suivantes lors de sa visite de terrain :\n\n" +
            $"NOTES BRUTES:\n{rawNotes}\n\n" +
            "Transforme ces notes en un constat d'audit structuré. " +
            "Réponds UNIQUEMENT avec ce JSON:\n" +
            "{\n  \"finding_type\": \"nc_critical|nc_major|nc_minor|observation|ofi\",\n" +
            "  \"title\": \"Titre court et précis (max 120 caractères)\",\n" +
            "  \"description\": \"Description complète du constat\",\n" +
            "  \"observed_evidence\": \"Preuves observées ou documents manquants\",\n" +
            "  \"regulatory_ref\": \"Référence exacte à la clause ou article applicable\",\n" +
            "  \"recommendation\": \"Action corrective recommandée\"\n}";

        return await CallClaudeJsonAsync<FindingSummary>(prompt, ct);
    }

    // 5.5 — CAPA suggester
    public async Task<CapaSuggestion?> SuggestCapaAsync(
        string findingType, string findingTitle, string? findingDescription,
        string referentialCode, string referentialName,
        CancellationToken ct = default)
    {
        var desc = string.IsNullOrEmpty(findingDescription) ? "(non renseignée)" : findingDescription;
        var prompt = $"Tu es un consultant expert en {referentialName} ({referentialCode}) spécialisé en amélioration continue.\n\n" +
            $"CONSTAT:\n- Type: {findingType}\n- Titre: {findingTitle}\n- Description: {desc}\n\n" +
            "Propose un plan d'action corrective (CAPA) précis et réaliste pour adresser ce constat.\n" +
            "Réponds UNIQUEMENT avec ce JSON:\n" +
            "{\n  \"title\": \"Titre de l'action corrective (max 120 caractères)\",\n" +
            "  \"description\": \"Description détaillée des étapes à mener\",\n" +
            "  \"root_cause\": \"Cause racine probable du problème\",\n" +
            "  \"action_type\": \"corrective|preventive|improvement\",\n" +
            "  \"priority\": \"critical|high|medium|low\",\n" +
            "  \"rationale\": \"Justification du choix de cette action et de sa priorité\"\n}";

        return await CallClaudeJsonAsync<CapaSuggestion>(prompt, ct);
    }

    // 5.6 — Question answering (in-audit context)
    public async Task<AuditAnswer?> AnswerAuditQuestionAsync(
        string userQuestion, string referentialCode, string referentialName,
        string? auditContext,
        CancellationToken ct = default)
    {
        var context = string.IsNullOrEmpty(auditContext) ? "" :
            $"\nCONTEXTE DE L'AUDIT:\n{auditContext}\n";
        var prompt = $"Tu es un expert du référentiel {referentialName} ({referentialCode}), " +
            "disponible pour répondre aux questions d'un auditeur en cours de mission.\n" +
            context + "\n" +
            $"QUESTION DE L'AUDITEUR:\n{userQuestion}\n\n" +
            "Réponds de façon précise, en citant les clauses exactes si applicable. " +
            "Réponds UNIQUEMENT avec ce JSON:\n" +
            "{\n  \"answer\": \"Réponse complète à la question\",\n" +
            "  \"references\": [\"Clause X.Y.Z\", \"Article N\"],\n" +
            "  \"confidence\": \"high|medium|low\",\n" +
            "  \"disclaimer\": \"Avertissement si la question dépasse le cadre du référentiel ou nécessite un expert juridique\"\n}";

        return await CallClaudeJsonAsync<AuditAnswer>(prompt, ct);
    }

    private async Task<T?> CallClaudeJsonAsync<T>(string prompt, CancellationToken ct)
    {
        try
        {
            var client = CreateClient();
            var request = new MessageParameters
            {
                Model = AnthropicModels.Claude3Haiku,
                MaxTokens = 2048,
                Messages = [new Message(RoleType.User, prompt)]
            };

            var response = await client.Messages.GetClaudeMessageAsync(request, ct);
            var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text;
            if (string.IsNullOrEmpty(text)) return default;

            var json = text.Trim();
            if (json.StartsWith("```"))
            {
                json = json[(json.IndexOf('\n') + 1)..];
                json = json[..json.LastIndexOf("```")].Trim();
            }

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claude API call failed for type {Type}", typeof(T).Name);
            return default;
        }
    }
}
