"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { FileSearch, AlertCircle, CheckCircle2, TrendingUp, ArrowRight, Clock } from "lucide-react";
import { auditsApi } from "@/lib/api/audits";
import type { Audit } from "@/lib/types";
import { cn } from "@/lib/utils";

export default function ClientDashboardPage() {
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
    .slice(0, 5);

  return (
    <div className="p-8 max-w-5xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-slate-900">Mon espace</h1>
        <p className="text-slate-500 text-sm mt-0.5">Suivez vos audits et actions correctives</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <StatCard icon={Clock} label="En cours" value={active} color="blue" loading={loading} />
        <StatCard icon={AlertCircle} label="À soumettre" value={submitted} color="amber" loading={loading} />
        <StatCard icon={CheckCircle2} label="Complétés" value={completed} color="emerald" loading={loading} />
        <StatCard
          icon={TrendingUp}
          label="Score moyen"
          value={audits.length ? `${Math.round(avgScore)}%` : "—"}
          color="purple"
          loading={loading}
        />
      </div>

      {/* Active audits needing attention */}
      <div className="bg-white rounded-2xl border border-slate-200 mb-6">
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
          <h2 className="font-semibold text-slate-900">Audits en attente de réponse</h2>
          <Link href="/client/audits" className="text-sm text-emerald-600 hover:text-emerald-700 flex items-center gap-1">
            Voir tout <ArrowRight className="h-3.5 w-3.5" />
          </Link>
        </div>

        {loading ? (
          <div className="divide-y divide-slate-100">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="px-6 py-4 animate-pulse">
                <div className="h-4 bg-slate-100 rounded w-2/3 mb-2" />
                <div className="h-3 bg-slate-100 rounded w-1/3" />
              </div>
            ))}
          </div>
        ) : recent.filter((a) => a.status === "active").length === 0 ? (
          <div className="px-6 py-10 text-center">
            <FileSearch className="h-9 w-9 text-slate-300 mx-auto mb-2" />
            <p className="text-sm text-slate-500">Aucun audit en attente</p>
          </div>
        ) : (
          <div className="divide-y divide-slate-100">
            {recent.filter((a) => a.status === "active").map((audit) => (
              <Link
                key={audit.id}
                href={`/client/audits/${audit.id}`}
                className="flex items-center gap-4 px-6 py-4 hover:bg-slate-50 transition-colors group"
              >
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-slate-900 text-sm truncate">{audit.title}</p>
                  <p className="text-xs text-slate-400 mt-0.5">
                    {audit.referential?.name ?? "—"}
                    {audit.deadline && ` · Échéance : ${new Date(audit.deadline).toLocaleDateString("fr-FR")}`}
                  </p>
                </div>
                <span className="text-xs font-medium px-2.5 py-1 rounded-full bg-blue-100 text-blue-700 shrink-0">
                  À compléter
                </span>
                <ArrowRight className="h-4 w-4 text-slate-300 group-hover:text-slate-500 shrink-0 transition-colors" />
              </Link>
            ))}
          </div>
        )}
      </div>

      {/* Compliance gauge */}
      {audits.filter((a) => a.complianceScore != null).length > 0 && (
        <div className="bg-white rounded-2xl border border-slate-200 p-6">
          <h2 className="font-semibold text-slate-900 mb-4">Score de conformité par audit</h2>
          <div className="space-y-3">
            {audits.filter((a) => a.complianceScore != null).map((audit) => (
              <div key={audit.id} className="flex items-center gap-3">
                <p className="text-sm text-slate-700 w-48 truncate shrink-0">{audit.title}</p>
                <div className="flex-1 h-2 bg-slate-100 rounded-full overflow-hidden">
                  <div
                    className={cn(
                      "h-2 rounded-full transition-all",
                      audit.complianceScore! >= 80 ? "bg-emerald-500" :
                      audit.complianceScore! >= 60 ? "bg-amber-500" : "bg-red-500"
                    )}
                    style={{ width: `${audit.complianceScore}%` }}
                  />
                </div>
                <span className={cn(
                  "text-sm font-bold w-12 text-right shrink-0",
                  audit.complianceScore! >= 80 ? "text-emerald-600" :
                  audit.complianceScore! >= 60 ? "text-amber-600" : "text-red-600"
                )}>
                  {audit.complianceScore}%
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function StatCard({
  icon: Icon, label, value, color, loading,
}: {
  icon: React.ElementType; label: string; value: number | string; color: string; loading: boolean;
}) {
  const colors: Record<string, { bg: string; icon: string; value: string }> = {
    blue:    { bg: "bg-blue-50",    icon: "text-blue-600",    value: "text-blue-700" },
    amber:   { bg: "bg-amber-50",   icon: "text-amber-600",   value: "text-amber-700" },
    emerald: { bg: "bg-emerald-50", icon: "text-emerald-600", value: "text-emerald-700" },
    purple:  { bg: "bg-purple-50",  icon: "text-purple-600",  value: "text-purple-700" },
  };
  const c = colors[color];
  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-5">
      <div className={cn("h-9 w-9 rounded-xl flex items-center justify-center mb-3", c.bg)}>
        <Icon className={cn("h-5 w-5", c.icon)} />
      </div>
      {loading ? (
        <div className="h-7 bg-slate-100 rounded w-12 animate-pulse mb-1" />
      ) : (
        <p className={cn("text-2xl font-bold", c.value)}>{value}</p>
      )}
      <p className="text-xs text-slate-500 mt-0.5">{label}</p>
    </div>
  );
}
