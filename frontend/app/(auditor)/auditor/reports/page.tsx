"use client";

import { useEffect, useState } from "react";
import { auditsApi } from "@/lib/api/audits";
import { api } from "@/lib/api/client";
import type { Audit, AuditReport } from "@/lib/types";
import { FileText, Download, Bot, Loader2, ExternalLink, Sparkles } from "lucide-react";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

interface AuditWithReport extends Audit {
  report?: AuditReport;
}

export default function ReportsPage() {
  const [audits, setAudits] = useState<AuditWithReport[]>([]);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState<string | null>(null);
  const [narrativeLoading, setNarrativeLoading] = useState<string | null>(null);

  useEffect(() => {
    auditsApi.getAll()
      .then(async (all) => {
        const completed = all.filter((a) => a.status === "completed" || a.status === "submitted");
        const withReports = await Promise.all(
          completed.map(async (a) => {
            try {
              const report = await api.get<AuditReport>(`/api/audits/${a.id}/report`);
              return { ...a, report };
            } catch {
              return a;
            }
          })
        );
        setAudits(withReports);
      })
      .finally(() => setLoading(false));
  }, []);

  const handleGenerate = async (auditId: string) => {
    setGenerating(auditId);
    try {
      const resp = await auditsApi.generateReport(auditId);
      if (!resp.ok) throw new Error("Échec génération PDF");
      const blob = await resp.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `audit-report-${auditId}.pdf`;
      link.click();
      URL.revokeObjectURL(url);
      const report = await auditsApi.getReport(auditId);
      setAudits((prev) =>
        prev.map((a) => (a.id === auditId ? { ...a, report } : a))
      );
      toast.success("Rapport généré et téléchargé");
    } catch {
      toast.error("Erreur lors de la génération");
    } finally {
      setGenerating(null);
    }
  };

  const handleNarrative = async (auditId: string) => {
    setNarrativeLoading(auditId);
    try {
      const narrative = await auditsApi.generateNarrative(auditId);
      setAudits((prev) => prev.map((a) =>
        a.id === auditId && a.report
          ? { ...a, report: { ...a.report, executiveSummary: narrative.executiveSummary } }
          : a
      ));
      toast.success("Synthèse IA générée");
    } catch {
      toast.error("Génération IA indisponible (clé API non configurée)");
    } finally {
      setNarrativeLoading(null);
    }
  };

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">Rapports</h1>
        <p className="text-slate-500 text-sm mt-0.5">
          Rapports d'audit générés par l'IA Claude (Anthropic)
        </p>
      </div>

      {loading ? (
        <div className="space-y-3">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="bg-white rounded-2xl border border-slate-200 p-5 animate-pulse">
              <div className="h-4 bg-slate-100 rounded w-1/2 mb-2" />
              <div className="h-3 bg-slate-100 rounded w-1/4" />
            </div>
          ))}
        </div>
      ) : audits.length === 0 ? (
        <div className="text-center py-16">
          <FileText className="h-10 w-10 text-slate-300 mx-auto mb-3" />
          <p className="text-slate-500 text-sm">Aucun audit complété pour le moment</p>
          <p className="text-slate-400 text-xs mt-1">
            Les rapports sont disponibles une fois un audit soumis ou complété
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {audits.map((audit) => (
            <div
              key={audit.id}
              className="bg-white rounded-2xl border border-slate-200 p-5"
            >
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <FileText className="h-4 w-4 text-slate-400 shrink-0" />
                    <h3 className="font-semibold text-slate-900 text-sm truncate">{audit.title}</h3>
                    {audit.report?.conformityScore != null && (
                      <span className={cn(
                        "text-xs font-bold px-2 py-0.5 rounded-full shrink-0",
                        audit.report.conformityScore >= 80 ? "text-emerald-700 bg-emerald-100" :
                        audit.report.conformityScore >= 60 ? "text-amber-700 bg-amber-100" :
                        "text-red-700 bg-red-100"
                      )}>
                        {audit.report.conformityScore.toFixed(0)}%
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-slate-400">
                    {audit.referentialName ?? "—"} ·{" "}
                    {new Date(audit.updatedAt).toLocaleDateString("fr-FR")}
                  </p>

                  {audit.report?.executiveSummary && (
                    <div className="mt-3 bg-purple-50 rounded-xl p-3.5 border border-purple-100">
                      <div className="flex items-center gap-1.5 mb-2">
                        <Bot className="h-3.5 w-3.5 text-purple-600" />
                        <span className="text-xs font-semibold text-purple-700">Synthèse</span>
                      </div>
                      <p className="text-xs text-purple-900 leading-relaxed line-clamp-4">
                        {audit.report.executiveSummary}
                      </p>
                    </div>
                  )}
                </div>

                <div className="flex flex-col gap-2 shrink-0">
                  {audit.report ? (
                    <>
                      <button
                        onClick={() => handleGenerate(audit.id)}
                        disabled={generating === audit.id}
                        className="flex items-center gap-1.5 text-sm font-medium text-blue-600 border border-blue-200 bg-blue-50 px-3 py-2 rounded-xl hover:bg-blue-100 transition-colors disabled:opacity-50"
                      >
                        {generating === audit.id ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
                        PDF
                      </button>
                      <button
                        onClick={() => handleNarrative(audit.id)}
                        disabled={narrativeLoading === audit.id}
                        className="flex items-center gap-1.5 text-sm font-medium text-purple-700 border border-purple-200 bg-purple-50 px-3 py-2 rounded-xl hover:bg-purple-100 transition-colors disabled:opacity-50"
                      >
                        {narrativeLoading === audit.id ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
                        Synthèse IA
                      </button>
                    </>
                  ) : (
                    <button
                      onClick={() => handleGenerate(audit.id)}
                      disabled={generating === audit.id}
                      className="flex items-center gap-1.5 text-sm font-medium text-purple-700 border border-purple-200 bg-purple-50 px-3 py-2 rounded-xl hover:bg-purple-100 disabled:opacity-60 transition-colors"
                    >
                      {generating === audit.id ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Bot className="h-4 w-4" />
                      )}
                      {generating === audit.id ? "Génération..." : "Générer rapport IA"}
                    </button>
                  )}
                  <a
                    href={`/auditor/audits/${audit.id}`}
                    className="flex items-center gap-1.5 text-xs font-medium text-slate-500 hover:text-slate-700 transition-colors"
                  >
                    <ExternalLink className="h-3.5 w-3.5" />
                    Voir l'audit
                  </a>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
