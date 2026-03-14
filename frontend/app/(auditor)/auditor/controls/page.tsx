"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Shield, Plus, Search, ChevronRight, GitBranch } from "lucide-react";
import { controlsApi } from "@/lib/api/controls";
import type { Control } from "@/lib/types";
import { cn } from "@/lib/utils";

const CATEGORIES = [
  { value: "access_control", label: "Contrôle d'accès" },
  { value: "data_protection", label: "Protection des données" },
  { value: "physical", label: "Physique & environnement" },
  { value: "organizational", label: "Organisationnel" },
  { value: "technical", label: "Technique" },
  { value: "legal", label: "Légal & conformité" },
];

const STATUS_COLORS: Record<string, string> = {
  draft: "bg-slate-100 text-slate-600",
  active: "bg-emerald-100 text-emerald-700",
  retired: "bg-red-100 text-red-600",
};

const STATUS_LABELS: Record<string, string> = {
  draft: "Brouillon",
  active: "Actif",
  retired: "Retiré",
};

export default function ControlsPage() {
  const [controls, setControls] = useState<Control[]>([]);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    controlsApi.getAll().then(setControls).finally(() => setLoading(false));
  }, []);

  const filtered = controls.filter((c) => {
    const matchSearch =
      !search ||
      c.code.toLowerCase().includes(search.toLowerCase()) ||
      c.title.toLowerCase().includes(search.toLowerCase());
    const matchStatus = !statusFilter || c.status === statusFilter;
    return matchSearch && matchStatus;
  });

  const activeCount = controls.filter((c) => c.status === "active").length;

  return (
    <div className="p-8 max-w-6xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Bibliothèque de contrôles</h1>
          <p className="text-slate-500 text-sm mt-0.5">
            {controls.length} contrôles · {activeCount} actifs
          </p>
        </div>
        <Link
          href="/auditor/controls/new"
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Nouveau contrôle
        </Link>
      </div>

      {/* Filters */}
      <div className="flex gap-3 mb-6 flex-wrap">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Rechercher..."
            className="pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white w-64"
          />
        </div>
        {(["all", "draft", "active", "retired"] as const).map((s) => (
          <button
            key={s}
            onClick={() => setStatusFilter(s === "all" ? null : s)}
            className={cn(
              "px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors",
              (s === "all" ? !statusFilter : statusFilter === s)
                ? "bg-blue-600 text-white border-blue-600"
                : "bg-white text-slate-600 border-slate-200 hover:border-slate-300"
            )}
          >
            {s === "all" ? "Tous" : STATUS_LABELS[s]}
          </button>
        ))}
      </div>

      {/* Table */}
      {loading ? (
        <div className="space-y-2">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="bg-white border border-slate-200 rounded-xl p-4 animate-pulse h-16" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-20">
          <Shield className="h-10 w-10 text-slate-300 mx-auto mb-3" />
          <p className="text-slate-500 text-sm">
            {controls.length === 0
              ? "Aucun contrôle défini. Créez votre premier contrôle."
              : "Aucun contrôle ne correspond à votre recherche."}
          </p>
        </div>
      ) : (
        <div className="bg-white border border-slate-200 rounded-2xl overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-100 bg-slate-50 text-xs text-slate-500 font-medium uppercase tracking-wide">
                <th className="text-left px-4 py-3">Code</th>
                <th className="text-left px-4 py-3">Titre</th>
                <th className="text-left px-4 py-3">Catégorie</th>
                <th className="text-left px-4 py-3">Responsable</th>
                <th className="text-left px-4 py-3">Mappings</th>
                <th className="text-left px-4 py-3">Statut</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filtered.map((control) => (
                <tr key={control.id} className="hover:bg-slate-50 transition-colors">
                  <td className="px-4 py-3 font-mono text-xs font-semibold text-blue-700">
                    {control.code}
                  </td>
                  <td className="px-4 py-3 font-medium text-slate-900 max-w-xs">
                    <p className="truncate">{control.title}</p>
                    {control.description && (
                      <p className="text-xs text-slate-400 truncate">{control.description}</p>
                    )}
                  </td>
                  <td className="px-4 py-3 text-slate-500">
                    {CATEGORIES.find((c) => c.value === control.category)?.label ?? control.category ?? "—"}
                  </td>
                  <td className="px-4 py-3 text-slate-500">{control.owner ?? "—"}</td>
                  <td className="px-4 py-3">
                    {control.mappingCount > 0 ? (
                      <span className="flex items-center gap-1 text-slate-600">
                        <GitBranch className="h-3.5 w-3.5" />
                        {control.mappingCount}
                      </span>
                    ) : (
                      <span className="text-slate-300">—</span>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <span className={cn("px-2 py-0.5 rounded-full text-xs font-medium", STATUS_COLORS[control.status])}>
                      {STATUS_LABELS[control.status]}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <Link
                      href={`/auditor/controls/${control.id}`}
                      className="text-blue-600 hover:text-blue-700 flex items-center gap-0.5"
                    >
                      <ChevronRight className="h-4 w-4" />
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
