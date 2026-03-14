"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import {
  Clock, AlertTriangle, CheckCircle2, TrendingUp, Plus, ArrowRight,
  AlertCircle, Repeat2, BarChart2, ShieldAlert,
} from "lucide-react";
import { analyticsApi } from "@/lib/api/analytics";
import type { DashboardData } from "@/lib/types";
import { cn } from "@/lib/utils";

const PRIORITY_COLORS: Record<string, string> = {
  critical: "bg-red-100 text-red-700",
  high:     "bg-orange-100 text-orange-700",
  medium:   "bg-amber-100 text-amber-700",
  low:      "bg-slate-100 text-slate-600",
};

export default function AuditorDashboardPage() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    analyticsApi.getDashboard().then(setData).finally(() => setLoading(false));
  }, []);

  if (loading) return <DashboardSkeleton />;
  if (!data) return <div className="p-8 text-red-500">Erreur de chargement</div>;

  const totalFindings =
    data.findingDistribution.ncCritical + data.findingDistribution.ncMajor +
    data.findingDistribution.ncMinor + data.findingDistribution.observation +
    data.findingDistribution.ofi;

  return (
    <div className="p-8 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Dashboard</h1>
          <p className="text-slate-500 text-sm mt-0.5">{data.totalAudits} audits au total</p>
        </div>
        <Link
          href="/auditor/audits/new"
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Nouvel audit
        </Link>
      </div>

      {/* KPI row */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-4">
        <KpiCard icon={Clock} label="En cours" value={data.active} color="blue" />
        <KpiCard icon={AlertTriangle} label="À réviser" value={data.submitted} color="amber" />
        <KpiCard icon={CheckCircle2} label="Complétés" value={data.completed} color="emerald" />
        <KpiCard icon={AlertCircle} label="En retard" value={data.overdue} color={data.overdue > 0 ? "red" : "slate"} />
        <KpiCard
          icon={TrendingUp}
          label="Score moyen"
          value={data.avgConformityScore != null ? `${data.avgConformityScore}%` : "—"}
          color="purple"
        />
      </div>

      <div className="grid grid-cols-3 gap-6">
        {/* Left column */}
        <div className="col-span-2 space-y-6">

          {/* Overdue audits */}
          {data.overdueAudits.length > 0 && (
            <section className="bg-red-50 border border-red-200 rounded-2xl overflow-hidden">
              <div className="px-5 py-3.5 border-b border-red-200 flex items-center gap-2">
                <AlertCircle className="h-4 w-4 text-red-500" />
                <h2 className="font-semibold text-red-700 text-sm">
                  Audits en retard ({data.overdueAudits.length})
                </h2>
              </div>
              <div className="divide-y divide-red-100">
                {data.overdueAudits.slice(0, 5).map((a) => (
                  <Link
                    key={a.id}
                    href={`/auditor/audits/${a.id}`}
                    className="flex items-center gap-4 px-5 py-3 hover:bg-red-100/50 transition-colors"
                  >
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-slate-800 truncate">{a.title}</p>
                      <p className="text-xs text-slate-500 mt-0.5">
                        {a.referentialCode} · Échéance {new Date(a.dueDate).toLocaleDateString("fr-FR")}
                      </p>
                    </div>
                    <span className="text-xs font-semibold text-red-600 bg-red-100 px-2 py-0.5 rounded-full shrink-0">
                      +{a.daysOverdue}j
                    </span>
                    <ArrowRight className="h-4 w-4 text-red-400 shrink-0" />
                  </Link>
                ))}
              </div>
            </section>
          )}

          {/* CAPA aging */}
          <section className="bg-white border border-slate-200 rounded-2xl overflow-hidden">
            <div className="px-5 py-3.5 border-b border-slate-100 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <ShieldAlert className="h-4 w-4 text-slate-400" />
                <h2 className="font-semibold text-slate-800 text-sm">
                  CAPAs ouvertes
                  <span className="ml-2 text-slate-400 font-normal">({data.capaAging.total})</span>
                </h2>
              </div>
              <Link href="/auditor/audits" className="text-xs text-blue-600 hover:underline">
                Voir audits
              </Link>
            </div>

            <div className="grid grid-cols-4 divide-x divide-slate-100 border-b border-slate-100">
              {(["critical", "high", "medium", "low"] as const).map((p) => (
                <div key={p} className="px-4 py-3 text-center">
                  <p className={cn("text-xl font-bold", {
                    critical: "text-red-600", high: "text-orange-600",
                    medium: "text-amber-600", low: "text-slate-600",
                  }[p])}>
                    {data.capaAging[p]}
                  </p>
                  <p className="text-xs text-slate-500 capitalize mt-0.5">{p}</p>
                </div>
              ))}
            </div>

            {data.capaAging.overdueItems.length > 0 ? (
              <>
                <p className="px-5 py-2 text-xs text-amber-700 bg-amber-50 font-medium border-b border-amber-100">
                  {data.capaAging.overdue} CAPA{data.capaAging.overdue !== 1 ? "s" : ""} en retard
                </p>
                <div className="divide-y divide-slate-100">
                  {data.capaAging.overdueItems.slice(0, 6).map((c) => (
                    <div key={c.id} className="flex items-center gap-3 px-5 py-3">
                      <span className={cn("text-[10px] font-semibold px-1.5 py-0.5 rounded-full shrink-0", PRIORITY_COLORS[c.priority])}>
                        {c.priority}
                      </span>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm text-slate-800 truncate">{c.title}</p>
                        <p className="text-xs text-slate-400 truncate">{c.auditTitle}</p>
                      </div>
                      {c.daysOverdue != null && (
                        <span className="text-xs text-red-600 shrink-0 font-medium">+{c.daysOverdue}j</span>
                      )}
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <div className="px-5 py-6 text-center">
                <CheckCircle2 className="h-7 w-7 text-emerald-400 mx-auto mb-1.5" />
                <p className="text-sm text-slate-500">Aucune CAPA en retard</p>
              </div>
            )}
          </section>
        </div>

        {/* Right column */}
        <div className="space-y-6">

          {/* Finding distribution */}
          {totalFindings > 0 && (
            <section className="bg-white border border-slate-200 rounded-2xl p-5">
              <div className="flex items-center gap-2 mb-4">
                <BarChart2 className="h-4 w-4 text-slate-400" />
                <h2 className="font-semibold text-slate-800 text-sm">Constats ouverts</h2>
              </div>
              <div className="space-y-2.5">
                {([
                  { key: "ncCritical", label: "NC Critique", color: "bg-red-500" },
                  { key: "ncMajor", label: "NC Majeure", color: "bg-orange-500" },
                  { key: "ncMinor", label: "NC Mineure", color: "bg-amber-400" },
                  { key: "observation", label: "Observation", color: "bg-blue-400" },
                  { key: "ofi", label: "OFI", color: "bg-slate-300" },
                ] as const).map(({ key, label, color }) => {
                  const val = data.findingDistribution[key];
                  const pct = totalFindings > 0 ? Math.round((val / totalFindings) * 100) : 0;
                  return (
                    <div key={key}>
                      <div className="flex justify-between text-xs mb-1">
                        <span className="text-slate-600">{label}</span>
                        <span className="font-semibold text-slate-800">{val}</span>
                      </div>
                      <div className="h-1.5 bg-slate-100 rounded-full overflow-hidden">
                        <div className={cn("h-full rounded-full", color)} style={{ width: `${pct}%` }} />
                      </div>
                    </div>
                  );
                })}
              </div>
            </section>
          )}

          {/* Conformity trend */}
          {data.conformityTrend.length > 0 && (
            <section className="bg-white border border-slate-200 rounded-2xl p-5">
              <div className="flex items-center gap-2 mb-4">
                <TrendingUp className="h-4 w-4 text-slate-400" />
                <h2 className="font-semibold text-slate-800 text-sm">Score — 6 mois</h2>
              </div>
              <div className="space-y-2">
                {data.conformityTrend.map((pt) => {
                  const [yr, mo] = pt.month.split("-");
                  const label = new Date(Number(yr), Number(mo) - 1).toLocaleDateString("fr-FR", { month: "short", year: "2-digit" });
                  const color = pt.avgScore >= 75 ? "bg-emerald-500" : pt.avgScore >= 50 ? "bg-amber-400" : "bg-red-400";
                  return (
                    <div key={pt.month} className="flex items-center gap-2">
                      <span className="text-xs text-slate-500 w-12 shrink-0">{label}</span>
                      <div className="flex-1 h-4 bg-slate-100 rounded overflow-hidden">
                        <div
                          className={cn("h-full rounded flex items-center justify-end pr-1.5", color)}
                          style={{ width: `${Math.max(pt.avgScore, 8)}%` }}
                        >
                          <span className="text-[10px] font-bold text-white">{pt.avgScore}%</span>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </section>
          )}

          {/* Repeat findings */}
          {data.repeatFindings.length > 0 && (
            <section className="bg-amber-50 border border-amber-200 rounded-2xl p-5">
              <div className="flex items-center gap-2 mb-3">
                <Repeat2 className="h-4 w-4 text-amber-600" />
                <h2 className="font-semibold text-amber-700 text-sm">Constats récurrents</h2>
              </div>
              <div className="space-y-2.5">
                {data.repeatFindings.map((rf, i) => (
                  <div key={i} className="bg-white border border-amber-100 rounded-xl p-3">
                    <p className="text-sm font-medium text-slate-800 truncate">{rf.title}</p>
                    <p className="text-xs text-amber-600 mt-0.5">
                      Détecté dans {rf.count} audits
                    </p>
                    <p className="text-[11px] text-slate-400 mt-1 line-clamp-1">
                      {rf.auditTitles.join(" · ")}
                    </p>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Empty state for right col */}
          {totalFindings === 0 && data.conformityTrend.length === 0 && data.repeatFindings.length === 0 && (
            <div className="bg-white border border-slate-200 rounded-2xl p-8 text-center">
              <BarChart2 className="h-8 w-8 text-slate-200 mx-auto mb-2" />
              <p className="text-sm text-slate-400">Les statistiques apparaîtront après vos premiers audits complétés.</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function KpiCard({
  icon: Icon, label, value, color,
}: {
  icon: React.ElementType;
  label: string;
  value: number | string;
  color: "blue" | "amber" | "emerald" | "red" | "purple" | "slate";
}) {
  const palette = {
    blue:    { bg: "bg-blue-50",    icon: "text-blue-600",    val: "text-blue-700" },
    amber:   { bg: "bg-amber-50",   icon: "text-amber-600",   val: "text-amber-700" },
    emerald: { bg: "bg-emerald-50", icon: "text-emerald-600", val: "text-emerald-700" },
    red:     { bg: "bg-red-50",     icon: "text-red-600",     val: "text-red-700" },
    purple:  { bg: "bg-purple-50",  icon: "text-purple-600",  val: "text-purple-700" },
    slate:   { bg: "bg-slate-50",   icon: "text-slate-500",   val: "text-slate-700" },
  }[color];

  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-5">
      <div className={cn("h-9 w-9 rounded-xl flex items-center justify-center mb-3", palette.bg)}>
        <Icon className={cn("h-5 w-5", palette.icon)} />
      </div>
      <p className={cn("text-2xl font-bold", palette.val)}>{value}</p>
      <p className="text-xs text-slate-500 mt-0.5">{label}</p>
    </div>
  );
}

function DashboardSkeleton() {
  return (
    <div className="p-8 max-w-7xl mx-auto space-y-6">
      <div className="h-8 bg-slate-100 rounded w-48 animate-pulse" />
      <div className="grid grid-cols-5 gap-4">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="bg-white rounded-2xl border border-slate-200 p-5 h-28 animate-pulse" />
        ))}
      </div>
      <div className="grid grid-cols-3 gap-6">
        <div className="col-span-2 space-y-4">
          <div className="h-40 bg-white rounded-2xl border border-slate-200 animate-pulse" />
          <div className="h-64 bg-white rounded-2xl border border-slate-200 animate-pulse" />
        </div>
        <div className="space-y-4">
          <div className="h-48 bg-white rounded-2xl border border-slate-200 animate-pulse" />
          <div className="h-32 bg-white rounded-2xl border border-slate-200 animate-pulse" />
        </div>
      </div>
    </div>
  );
}
