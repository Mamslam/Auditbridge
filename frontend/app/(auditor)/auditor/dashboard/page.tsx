"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  ClipboardCheck,
  AlertTriangle,
  TrendingUp,
  Clock,
  Plus,
  ArrowRight,
  CheckCircle2,
  Circle,
  XCircle,
} from "lucide-react";
import { auditsApi } from "@/lib/api/audits";
import type { Audit, AuditStatus } from "@/lib/types";
import { cn } from "@/lib/utils";

const STATUS_CONFIG: Record<AuditStatus, { label: string; color: string; icon: React.ElementType }> = {
  draft:     { label: "Brouillon",   color: "text-slate-500 bg-slate-100",   icon: Circle },
  active:    { label: "En cours",    color: "text-blue-700 bg-blue-100",     icon: Clock },
  submitted: { label: "Soumis",      color: "text-amber-700 bg-amber-100",   icon: AlertTriangle },
  completed: { label: "Complété",    color: "text-emerald-700 bg-emerald-100", icon: CheckCircle2 },
  archived:  { label: "Archivé",     color: "text-slate-400 bg-slate-50",    icon: XCircle },
};

export default function AuditorDashboardPage() {
  const [audits, setAudits] = useState<Audit[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    auditsApi.getAll()
      .then(setAudits)
      .catch(() => setAudits([]))
      .finally(() => setLoading(false));
  }, []);

  const active = audits.filter((a) => a.status === "active").length;
  const submitted = audits.filter((a) => a.status === "submitted").length;
  const completed = audits.filter((a) => a.status === "completed").length;
  const avgScore = audits
    .filter((a) => a.complianceScore != null)
    .reduce((sum, a, _, arr) => sum + (a.complianceScore! / arr.length), 0);

  const recent = [...audits]
    .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
    .slice(0, 6);

  return (
    <div className="p-8 max-w-6xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Dashboard</h1>
          <p className="text-slate-500 text-sm mt-0.5">Vue d'ensemble de vos audits</p>
        </div>
        <Link
          href="/auditor/audits/new"
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Nouvel audit
        </Link>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <StatCard icon={Clock} label="En cours" value={active} color="blue" loading={loading} />
        <StatCard icon={AlertTriangle} label="À réviser" value={submitted} color="amber" loading={loading} />
        <StatCard icon={CheckCircle2} label="Complétés" value={completed} color="emerald" loading={loading} />
        <StatCard
          icon={TrendingUp}
          label="Score moyen"
          value={audits.length ? `${Math.round(avgScore)}%` : "—"}
          color="purple"
          loading={loading}
        />
      </div>

      {/* Recent audits */}
      <div className="bg-white rounded-2xl border border-slate-200">
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
          <h2 className="font-semibold text-slate-900">Audits récents</h2>
          <Link href="/auditor/audits" className="text-sm text-blue-600 hover:text-blue-700 flex items-center gap-1">
            Voir tout <ArrowRight className="h-3.5 w-3.5" />
          </Link>
        </div>

        {loading ? (
          <div className="divide-y divide-slate-100">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="px-6 py-4 flex items-center gap-4">
                <div className="h-4 bg-slate-100 rounded w-48 animate-pulse" />
                <div className="h-5 bg-slate-100 rounded-full w-20 animate-pulse ml-auto" />
              </div>
            ))}
          </div>
        ) : recent.length === 0 ? (
          <div className="px-6 py-12 text-center">
            <ClipboardCheck className="h-10 w-10 text-slate-300 mx-auto mb-3" />
            <p className="text-slate-500 text-sm">Aucun audit pour le moment</p>
            <Link href="/auditor/audits/new" className="text-blue-600 text-sm font-medium mt-1 inline-block hover:underline">
              Créer votre premier audit
            </Link>
          </div>
        ) : (
          <div className="divide-y divide-slate-100">
            {recent.map((audit) => {
              const cfg = STATUS_CONFIG[audit.status];
              const Icon = cfg.icon;
              return (
                <Link
                  key={audit.id}
                  href={`/auditor/audits/${audit.id}`}
                  className="flex items-center gap-4 px-6 py-4 hover:bg-slate-50 transition-colors group"
                >
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-slate-900 truncate text-sm">{audit.title}</p>
                    <p className="text-xs text-slate-400 mt-0.5">
                      {audit.referential?.name ?? "—"} · {new Date(audit.updatedAt).toLocaleDateString("fr-FR")}
                    </p>
                  </div>
                  {audit.complianceScore != null && (
                    <div className={cn("text-sm font-bold", audit.complianceScore >= 80 ? "text-emerald-600" : audit.complianceScore >= 60 ? "text-amber-600" : "text-red-600")}>
                      {audit.complianceScore}%
                    </div>
                  )}
                  <span className={cn("flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full", cfg.color)}>
                    <Icon className="h-3 w-3" />
                    {cfg.label}
                  </span>
                  <ArrowRight className="h-4 w-4 text-slate-300 group-hover:text-slate-500 shrink-0 transition-colors" />
                </Link>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

function StatCard({
  icon: Icon,
  label,
  value,
  color,
  loading,
}: {
  icon: React.ElementType;
  label: string;
  value: number | string;
  color: "blue" | "amber" | "emerald" | "purple";
  loading: boolean;
}) {
  const colors = {
    blue:    { bg: "bg-blue-50",    icon: "text-blue-600",    value: "text-blue-700" },
    amber:   { bg: "bg-amber-50",   icon: "text-amber-600",   value: "text-amber-700" },
    emerald: { bg: "bg-emerald-50", icon: "text-emerald-600", value: "text-emerald-700" },
    purple:  { bg: "bg-purple-50",  icon: "text-purple-600",  value: "text-purple-700" },
  }[color];

  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-5">
      <div className={cn("h-9 w-9 rounded-xl flex items-center justify-center mb-3", colors.bg)}>
        <Icon className={cn("h-5 w-5", colors.icon)} />
      </div>
      {loading ? (
        <div className="h-7 bg-slate-100 rounded w-12 animate-pulse mb-1" />
      ) : (
        <p className={cn("text-2xl font-bold", colors.value)}>{value}</p>
      )}
      <p className="text-xs text-slate-500 mt-0.5">{label}</p>
    </div>
  );
}
