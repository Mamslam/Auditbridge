"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import {
  ArrowLeft, Zap, Send, CheckCircle2, Copy, ExternalLink,
  Clock, AlertTriangle, Circle, Loader2, Bot, ChevronDown, ChevronRight
} from "lucide-react";
import { auditsApi } from "@/lib/api/audits";
import type { AuditDetail, AuditStatus, ConformityRating } from "@/lib/types";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

const STATUS_CONFIG: Record<AuditStatus, { label: string; color: string; icon: React.ElementType }> = {
  draft:     { label: "Brouillon",  color: "text-slate-500 bg-slate-100",     icon: Circle },
  active:    { label: "En cours",   color: "text-blue-700 bg-blue-100",       icon: Clock },
  submitted: { label: "Soumis",     color: "text-amber-700 bg-amber-100",     icon: AlertTriangle },
  completed: { label: "Complété",   color: "text-emerald-700 bg-emerald-100", icon: CheckCircle2 },
  archived:  { label: "Archivé",    color: "text-slate-400 bg-slate-50",      icon: Circle },
};

const CONFORMITY_CONFIG: Record<ConformityRating, { label: string; color: string }> = {
  compliant: { label: "Conforme",     color: "text-emerald-700 bg-emerald-100 border-emerald-200" },
  minor:     { label: "Mineur",       color: "text-amber-700 bg-amber-100 border-amber-200" },
  major:     { label: "Majeur",       color: "text-orange-700 bg-orange-100 border-orange-200" },
  critical:  { label: "Critique",     color: "text-red-700 bg-red-100 border-red-200" },
  na:        { label: "N/A",          color: "text-slate-500 bg-slate-100 border-slate-200" },
};

export default function AuditDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [audit, setAudit] = useState<AuditDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [activating, setActivating] = useState(false);
  const [completing, setCompleting] = useState(false);
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set());

  useEffect(() => {
    auditsApi.getById(id)
      .then((data) => {
        setAudit(data);
        // Expand all sections by default
        const sections = new Set(
          (data.responses ?? []).map((r) => r.question?.sectionId ?? "").filter(Boolean)
        );
        setExpandedSections(sections);
      })
      .finally(() => setLoading(false));
  }, [id]);

  const handleActivate = async () => {
    setActivating(true);
    try {
      const updated = await auditsApi.activate(id);
      setAudit((prev) => prev ? { ...prev, ...updated } : prev);
      toast.success("Audit activé — lien client généré");
    } catch {
      toast.error("Erreur lors de l'activation");
    } finally {
      setActivating(false);
    }
  };

  const handleComplete = async () => {
    setCompleting(true);
    try {
      const updated = await auditsApi.complete(id);
      setAudit((prev) => prev ? { ...prev, ...updated } : prev);
      toast.success("Audit marqué comme complété");
    } catch {
      toast.error("Erreur lors de la complétion");
    } finally {
      setCompleting(false);
    }
  };

  const handleGenerateReport = async () => {
    try {
      await auditsApi.generateReport(id);
      toast.success("Rapport IA en cours de génération...");
    } catch {
      toast.error("Erreur lors de la génération du rapport");
    }
  };

  const copyClientLink = () => {
    if (!audit?.clientToken) return;
    const url = `${window.location.origin}/audit/${audit.clientToken}`;
    navigator.clipboard.writeText(url);
    toast.success("Lien copié !");
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
      </div>
    );
  }

  if (!audit) {
    return (
      <div className="p-8 text-center">
        <p className="text-slate-500">Audit introuvable</p>
        <Link href="/auditor/audits" className="text-blue-600 text-sm mt-2 inline-block">← Retour aux audits</Link>
      </div>
    );
  }

  const cfg = STATUS_CONFIG[audit.status];
  const StatusIcon = cfg.icon;

  // Group responses by section
  const responsesBySectionId = (audit.responses ?? []).reduce<Record<string, typeof audit.responses>>((acc, r) => {
    const secId = r.question?.sectionId ?? "other";
    return { ...acc, [secId]: [...(acc[secId] ?? []), r] };
  }, {});

  const answered = (audit.responses ?? []).filter((r) => r.answerValue || r.conformity).length;
  const total = (audit.responses ?? []).length;
  const progress = total > 0 ? Math.round((answered / total) * 100) : 0;

  return (
    <div className="p-8 max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex items-start gap-4 mb-6">
        <Link href="/auditor/audits" className="text-slate-400 hover:text-slate-600 mt-1">
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-3 flex-wrap">
            <h1 className="text-xl font-bold text-slate-900 truncate">{audit.title}</h1>
            <span className={cn("flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full", cfg.color)}>
              <StatusIcon className="h-3 w-3" />
              {cfg.label}
            </span>
          </div>
          <p className="text-sm text-slate-400 mt-0.5">
            {audit.referential?.name ?? "—"} · {audit.referential?.version ?? ""}
            {audit.deadline && ` · Échéance : ${new Date(audit.deadline).toLocaleDateString("fr-FR")}`}
          </p>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2 shrink-0 flex-wrap justify-end">
          {audit.status === "draft" && (
            <button
              onClick={handleActivate}
              disabled={activating}
              className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-xl text-sm font-semibold hover:bg-blue-700 disabled:opacity-70 transition-colors"
            >
              {activating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Zap className="h-4 w-4" />}
              Activer
            </button>
          )}
          {audit.status === "active" && audit.clientToken && (
            <button
              onClick={copyClientLink}
              className="flex items-center gap-2 border border-slate-200 bg-white text-slate-700 px-4 py-2 rounded-xl text-sm font-semibold hover:border-slate-300 transition-colors"
            >
              <Copy className="h-4 w-4" />
              Copier le lien client
            </button>
          )}
          {audit.status === "submitted" && (
            <button
              onClick={handleComplete}
              disabled={completing}
              className="flex items-center gap-2 bg-emerald-600 text-white px-4 py-2 rounded-xl text-sm font-semibold hover:bg-emerald-700 disabled:opacity-70 transition-colors"
            >
              {completing ? <Loader2 className="h-4 w-4 animate-spin" /> : <CheckCircle2 className="h-4 w-4" />}
              Compléter
            </button>
          )}
          {(audit.status === "submitted" || audit.status === "completed") && (
            <button
              onClick={handleGenerateReport}
              className="flex items-center gap-2 border border-purple-200 bg-purple-50 text-purple-700 px-4 py-2 rounded-xl text-sm font-semibold hover:bg-purple-100 transition-colors"
            >
              <Bot className="h-4 w-4" />
              Rapport IA
            </button>
          )}
        </div>
      </div>

      {/* Score + progress */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div className="bg-white rounded-2xl border border-slate-200 p-4">
          <p className="text-xs text-slate-400 mb-1">Score de conformité</p>
          <p className={cn("text-2xl font-bold",
            audit.complianceScore == null ? "text-slate-300" :
            audit.complianceScore >= 80 ? "text-emerald-600" :
            audit.complianceScore >= 60 ? "text-amber-600" : "text-red-600"
          )}>
            {audit.complianceScore != null ? `${audit.complianceScore}%` : "—"}
          </p>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 p-4">
          <p className="text-xs text-slate-400 mb-1">Progression</p>
          <p className="text-2xl font-bold text-slate-900">{progress}%</p>
          <div className="w-full h-1.5 bg-slate-100 rounded-full mt-2">
            <div className="h-1.5 bg-blue-500 rounded-full transition-all" style={{ width: `${progress}%` }} />
          </div>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 p-4">
          <p className="text-xs text-slate-400 mb-1">Questions répondues</p>
          <p className="text-2xl font-bold text-slate-900">{answered}/{total}</p>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 p-4">
          <p className="text-xs text-slate-400 mb-1">CAPAs ouvertes</p>
          <p className="text-2xl font-bold text-amber-600">
            {(audit.capas ?? []).filter((c) => c.status === "open" || c.status === "in_progress").length}
          </p>
        </div>
      </div>

      {/* Client token info */}
      {audit.clientToken && audit.status === "active" && (
        <div className="bg-blue-50 border border-blue-200 rounded-2xl p-4 mb-6 flex items-center justify-between gap-4">
          <div>
            <p className="text-sm font-semibold text-blue-900">Lien client actif</p>
            <p className="text-xs text-blue-600 mt-0.5 font-mono truncate">
              {typeof window !== "undefined" ? `${window.location.origin}/audit/${audit.clientToken}` : `…/audit/${audit.clientToken}`}
            </p>
          </div>
          <div className="flex gap-2 shrink-0">
            <button onClick={copyClientLink} className="p-2 text-blue-600 hover:bg-blue-100 rounded-lg transition-colors">
              <Copy className="h-4 w-4" />
            </button>
            <a href={`/audit/${audit.clientToken}`} target="_blank" rel="noopener noreferrer"
              className="p-2 text-blue-600 hover:bg-blue-100 rounded-lg transition-colors">
              <ExternalLink className="h-4 w-4" />
            </a>
          </div>
        </div>
      )}

      {/* Responses grouped by section */}
      {total > 0 && (
        <div className="space-y-3">
          <h2 className="font-semibold text-slate-900 text-sm">Réponses</h2>
          {Object.entries(responsesBySectionId).map(([sectionId, responses]) => {
            const sectionTitle = responses[0]?.question ? `Section` : "Questions";
            const expanded = expandedSections.has(sectionId);
            return (
              <div key={sectionId} className="bg-white rounded-2xl border border-slate-200 overflow-hidden">
                <button
                  onClick={() => setExpandedSections((prev) => {
                    const next = new Set(prev);
                    expanded ? next.delete(sectionId) : next.add(sectionId);
                    return next;
                  })}
                  className="w-full flex items-center gap-3 px-5 py-3.5 text-left hover:bg-slate-50 transition-colors border-b border-slate-100"
                >
                  {expanded ? <ChevronDown className="h-4 w-4 text-slate-400" /> : <ChevronRight className="h-4 w-4 text-slate-400" />}
                  <span className="text-sm font-semibold text-slate-900 flex-1">{sectionTitle}</span>
                  <span className="text-xs text-slate-400">{responses.length} questions</span>
                </button>

                {expanded && (
                  <div className="divide-y divide-slate-50">
                    {responses.map((resp) => {
                      const critCfg = resp.conformity ? CONFORMITY_CONFIG[resp.conformity] : null;
                      return (
                        <div key={resp.id} className="px-5 py-4">
                          <div className="flex items-start gap-3">
                            <div className="flex-1 min-w-0">
                              <p className="text-sm text-slate-800">{resp.question?.text ?? `Question ${resp.questionId.slice(0, 8)}`}</p>
                              {resp.answerValue && (
                                <p className="text-xs text-slate-500 mt-1">Réponse : <span className="font-medium">{resp.answerValue}</span></p>
                              )}
                              {resp.comment && (
                                <p className="text-xs text-slate-400 mt-0.5 italic">"{resp.comment}"</p>
                              )}
                              {resp.aiAnalysis && (
                                <div className="flex items-start gap-1.5 mt-2 bg-purple-50 rounded-lg p-2.5">
                                  <Bot className="h-3.5 w-3.5 text-purple-600 mt-0.5 shrink-0" />
                                  <p className="text-xs text-purple-700">{resp.aiAnalysis}</p>
                                </div>
                              )}
                            </div>
                            {critCfg && (
                              <span className={cn("text-xs font-medium px-2 py-1 rounded-full border shrink-0", critCfg.color)}>
                                {critCfg.label}
                              </span>
                            )}
                            {resp.isFlagged && (
                              <AlertTriangle className="h-4 w-4 text-amber-500 shrink-0" />
                            )}
                          </div>

                          {/* Conformity buttons (auditor can set) */}
                          {(audit.status === "submitted" || audit.status === "active") && (
                            <div className="flex gap-2 mt-3 flex-wrap">
                              {(["compliant", "minor", "major", "critical", "na"] as ConformityRating[]).map((conf) => {
                                const c = CONFORMITY_CONFIG[conf];
                                return (
                                  <button
                                    key={conf}
                                    onClick={async () => {
                                      await auditsApi.setConformity(id, resp.id, { conformity: conf });
                                      setAudit((prev) => prev ? {
                                        ...prev,
                                        responses: prev.responses.map((r) =>
                                          r.id === resp.id ? { ...r, conformity: conf } : r
                                        ),
                                      } : prev);
                                    }}
                                    className={cn(
                                      "text-xs px-2.5 py-1 rounded-full border transition-colors font-medium",
                                      resp.conformity === conf
                                        ? cn(c.color, "ring-2 ring-offset-1 ring-current")
                                        : "text-slate-500 bg-white border-slate-200 hover:border-slate-300"
                                    )}
                                  >
                                    {c.label}
                                  </button>
                                );
                              })}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* CAPAs */}
      {(audit.capas ?? []).length > 0 && (
        <div className="mt-6">
          <h2 className="font-semibold text-slate-900 text-sm mb-3">CAPAs ({audit.capas.length})</h2>
          <div className="space-y-2">
            {audit.capas.map((capa) => (
              <div key={capa.id} className="bg-white rounded-xl border border-slate-200 px-4 py-3 flex items-start gap-3">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-slate-900">{capa.title}</p>
                  {capa.description && <p className="text-xs text-slate-500 mt-0.5">{capa.description}</p>}
                </div>
                <span className={cn(
                  "text-xs font-medium px-2 py-1 rounded-full border shrink-0",
                  capa.status === "open" ? "text-red-700 bg-red-50 border-red-200" :
                  capa.status === "verified" ? "text-emerald-700 bg-emerald-50 border-emerald-200" :
                  "text-amber-700 bg-amber-50 border-amber-200"
                )}>
                  {capa.status}
                </span>
                {capa.aiGenerated && <Bot className="h-4 w-4 text-purple-400 shrink-0" />}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
