"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Plus, Search, ClipboardCheck, Clock, CheckCircle2, AlertTriangle, Circle, XCircle, ArrowRight } from "lucide-react";
import { auditsApi } from "@/lib/api/audits";
import type { Audit, AuditStatus } from "@/lib/types";
import { cn } from "@/lib/utils";

const STATUS_CONFIG: Record<AuditStatus, { label: string; color: string; icon: React.ElementType }> = {
  draft:     { label: "Brouillon",  color: "text-slate-500 bg-slate-100",     icon: Circle },
  active:    { label: "En cours",   color: "text-blue-700 bg-blue-100",       icon: Clock },
  submitted: { label: "Soumis",     color: "text-amber-700 bg-amber-100",     icon: AlertTriangle },
  completed: { label: "Complété",   color: "text-emerald-700 bg-emerald-100", icon: CheckCircle2 },
  archived:  { label: "Archivé",    color: "text-slate-400 bg-slate-50",      icon: XCircle },
};

export default function AuditsPage() {
  const [audits, setAudits] = useState<Audit[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [filterStatus, setFilterStatus] = useState<AuditStatus | "all">("all");

  useEffect(() => {
    auditsApi.getAll()
      .then(setAudits)
      .catch(() => setAudits([]))
      .finally(() => setLoading(false));
  }, []);

  const filtered = audits.filter((a) => {
    const matchSearch = !search || a.title.toLowerCase().includes(search.toLowerCase());
    const matchStatus = filterStatus === "all" || a.status === filterStatus;
    return matchSearch && matchStatus;
  });

  return (
    <div className="p-8 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Audits</h1>
          <p className="text-slate-500 text-sm mt-0.5">{audits.length} audit{audits.length !== 1 ? "s" : ""} au total</p>
        </div>
        <Link
          href="/auditor/audits/new"
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Nouvel audit
        </Link>
      </div>

      {/* Filters */}
      <div className="flex gap-3 mb-6 flex-wrap">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Rechercher..."
            className="pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white w-56"
          />
        </div>
        {(["all", "draft", "active", "submitted", "completed"] as const).map((s) => (
          <button
            key={s}
            onClick={() => setFilterStatus(s)}
            className={cn(
              "px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors",
              filterStatus === s
                ? "bg-blue-600 text-white border-blue-600"
                : "bg-white text-slate-600 border-slate-200 hover:border-slate-300"
            )}
          >
            {s === "all" ? "Tous" : STATUS_CONFIG[s].label}
          </button>
        ))}
      </div>

      {/* Table */}
      <div className="bg-white rounded-2xl border border-slate-200 overflow-hidden">
        <div className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-4 px-6 py-3 border-b border-slate-100 text-xs font-medium text-slate-400 uppercase tracking-wider">
          <span>Titre</span>
          <span>Référentiel</span>
          <span>Échéance</span>
          <span>Score</span>
          <span>Statut</span>
        </div>

        {loading ? (
          <div className="divide-y divide-slate-100">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-4 px-6 py-4 animate-pulse">
                <div className="h-4 bg-slate-100 rounded w-3/4" />
                <div className="h-4 bg-slate-100 rounded w-24" />
                <div className="h-4 bg-slate-100 rounded w-20" />
                <div className="h-4 bg-slate-100 rounded w-12" />
                <div className="h-5 bg-slate-100 rounded-full w-20" />
              </div>
            ))}
          </div>
        ) : filtered.length === 0 ? (
          <div className="py-16 text-center">
            <ClipboardCheck className="h-10 w-10 text-slate-300 mx-auto mb-3" />
            <p className="text-slate-500 text-sm">Aucun audit trouvé</p>
          </div>
        ) : (
          <div className="divide-y divide-slate-100">
            {filtered.map((audit) => {
              const cfg = STATUS_CONFIG[audit.status];
              const Icon = cfg.icon;
              return (
                <Link
                  key={audit.id}
                  href={`/auditor/audits/${audit.id}`}
                  className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-4 items-center px-6 py-4 hover:bg-slate-50 transition-colors group"
                >
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-slate-900 truncate">{audit.title}</p>
                    <p className="text-xs text-slate-400 mt-0.5">{new Date(audit.createdAt).toLocaleDateString("fr-FR")}</p>
                  </div>
                  <span className="text-sm text-slate-600 whitespace-nowrap">{audit.referentialName ?? "—"}</span>
                  <span className="text-sm text-slate-500 whitespace-nowrap">
                    {audit.dueDate ? new Date(audit.dueDate).toLocaleDateString("fr-FR") : "—"}
                  </span>
                  <span className="text-sm font-bold whitespace-nowrap text-slate-400">—</span>
                  <span className={cn("flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full whitespace-nowrap", cfg.color)}>
                    <Icon className="h-3 w-3" />
                    {cfg.label}
                  </span>
                </Link>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
