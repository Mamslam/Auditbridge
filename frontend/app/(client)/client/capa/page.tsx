"use client";

import { useEffect, useState } from "react";
import { auditsApi } from "@/lib/api/audits";
import type { Audit, AuditCapa, CapaStatus } from "@/lib/types";
import { AlertCircle, CheckCircle2, Clock, Bot, Calendar, Loader2, X, PlayCircle } from "lucide-react";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

const STATUS_CONFIG: Record<CapaStatus, { label: string; color: string; icon: React.ElementType }> = {
  open:                 { label: "Ouverte",       color: "text-red-700 bg-red-100 border-red-200",             icon: AlertCircle },
  in_progress:          { label: "En cours",       color: "text-amber-700 bg-amber-100 border-amber-200",       icon: Clock },
  pending_verification: { label: "Vérification",   color: "text-blue-700 bg-blue-100 border-blue-200",          icon: Clock },
  verified:             { label: "Vérifié",        color: "text-emerald-700 bg-emerald-100 border-emerald-200", icon: CheckCircle2 },
  cancelled:            { label: "Annulé",         color: "text-slate-500 bg-slate-100 border-slate-200",       icon: X },
};

interface CapaWithAudit extends AuditCapa {
  auditTitle?: string;
}

export default function CapaPage() {
  const [capas, setCapas] = useState<CapaWithAudit[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<CapaStatus | "all">("all");
  const [transitioning, setTransitioning] = useState<string | null>(null);

  useEffect(() => {
    auditsApi.getAll()
      .then(async (audits: Audit[]) => {
        const all: CapaWithAudit[] = [];
        for (const audit of audits) {
          try {
            const detail = await auditsApi.getById(audit.id);
            for (const capa of detail.capas ?? []) {
              all.push({ ...capa, auditTitle: audit.title });
            }
          } catch { /* skip */ }
        }
        setCapas(all);
      })
      .finally(() => setLoading(false));
  }, []);

  const transition = async (capa: CapaWithAudit, status: CapaStatus) => {
    setTransitioning(capa.id);
    try {
      const updated = await auditsApi.updateCapaStatus(capa.auditId, capa.id, status);
      setCapas((prev) => prev.map((c) => c.id === capa.id ? { ...c, ...updated } : c));
      toast.success("CAPA mise à jour");
    } catch {
      toast.error("Erreur lors de la mise à jour");
    } finally {
      setTransitioning(null);
    }
  };

  const filtered = capas.filter((c) => filter === "all" || c.status === filter);
  const openCount = capas.filter((c) => c.status === "open" || c.status === "in_progress").length;

  return (
    <div className="p-8 max-w-4xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">CAPAs</h1>
          <p className="text-slate-500 text-sm mt-0.5">
            Actions correctives et préventives
            {openCount > 0 && (
              <span className="ml-2 text-red-600 font-medium">({openCount} ouverte{openCount > 1 ? "s" : ""})</span>
            )}
          </p>
        </div>
      </div>

      <div className="flex gap-2 flex-wrap mb-6">
        {(["all", "open", "in_progress", "pending_verification", "verified", "cancelled"] as const).map((s) => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            className={cn(
              "px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors",
              filter === s
                ? "bg-blue-600 text-white border-blue-600"
                : "bg-white text-slate-600 border-slate-200 hover:border-slate-300"
            )}
          >
            {s === "all" ? "Toutes" : STATUS_CONFIG[s].label}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="space-y-3">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="bg-white rounded-2xl border border-slate-200 p-5 animate-pulse">
              <div className="h-4 bg-slate-100 rounded w-2/3 mb-2" />
              <div className="h-3 bg-slate-100 rounded w-1/3" />
            </div>
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-16">
          <CheckCircle2 className="h-10 w-10 text-emerald-300 mx-auto mb-3" />
          <p className="text-slate-500 text-sm">Aucune CAPA{filter !== "all" ? " dans ce statut" : ""}</p>
        </div>
      ) : (
        <div className="space-y-3">
          {filtered.map((capa) => {
            const cfg = STATUS_CONFIG[capa.status];
            const Icon = cfg.icon;
            const isBusy = transitioning === capa.id;
            return (
              <div key={capa.id} className="bg-white rounded-2xl border border-slate-200 p-5">
                <div className="flex items-start gap-4">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1 flex-wrap">
                      <h3 className="font-semibold text-slate-900 text-sm">{capa.title}</h3>
                      {capa.aiGenerated && (
                        <span className="flex items-center gap-1 text-[10px] font-medium text-purple-700 bg-purple-100 px-1.5 py-0.5 rounded-full">
                          <Bot className="h-2.5 w-2.5" /> IA
                        </span>
                      )}
                      <span className={cn(
                        "text-[10px] font-semibold px-2 py-0.5 rounded-full uppercase tracking-wide",
                        capa.priority === "critical" ? "text-red-700 bg-red-100" :
                        capa.priority === "high" ? "text-orange-700 bg-orange-100" :
                        "text-slate-600 bg-slate-100"
                      )}>
                        {capa.priority}
                      </span>
                    </div>
                    {capa.description && (
                      <p className="text-sm text-slate-500 mb-2">{capa.description}</p>
                    )}
                    <div className="flex items-center gap-3 text-xs text-slate-400 flex-wrap">
                      {capa.auditTitle && <span>Audit : {capa.auditTitle}</span>}
                      {capa.dueDate && (
                        <span className={cn(
                          "flex items-center gap-1",
                          new Date(capa.dueDate) < new Date() && capa.status !== "verified" ? "text-red-500" : ""
                        )}>
                          <Calendar className="h-3 w-3" />
                          {new Date(capa.dueDate).toLocaleDateString("fr-FR")}
                        </span>
                      )}
                      {capa.assignedToEmail && <span>{capa.assignedToEmail}</span>}
                    </div>
                  </div>

                  <div className="flex flex-col items-end gap-2 shrink-0">
                    <span className={cn("flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full border", cfg.color)}>
                      <Icon className="h-3 w-3" />
                      {cfg.label}
                    </span>
                    {capa.status === "open" && (
                      <button
                        onClick={() => transition(capa, "in_progress")}
                        disabled={isBusy}
                        className="flex items-center gap-1.5 text-xs font-medium text-blue-700 border border-blue-200 bg-blue-50 px-3 py-1.5 rounded-xl hover:bg-blue-100 disabled:opacity-60 transition-colors"
                      >
                        {isBusy ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <PlayCircle className="h-3.5 w-3.5" />}
                        Démarrer
                      </button>
                    )}
                    {(capa.status === "open" || capa.status === "in_progress") && (
                      <button
                        onClick={() => transition(capa, "pending_verification")}
                        disabled={isBusy}
                        className="flex items-center gap-1.5 text-xs font-medium text-emerald-700 border border-emerald-200 bg-emerald-50 px-3 py-1.5 rounded-xl hover:bg-emerald-100 disabled:opacity-60 transition-colors"
                      >
                        {isBusy ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <CheckCircle2 className="h-3.5 w-3.5" />}
                        Marquer complétée
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
