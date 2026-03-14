"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Shield, CheckCircle2, AlertCircle, Circle } from "lucide-react";
import { controlsApi } from "@/lib/api/controls";
import type { ReferentialCoverage, QuestionCoverage } from "@/lib/types";
import { cn } from "@/lib/utils";

function CoverageBar({ pct }: { pct: number }) {
  const color = pct >= 75 ? "bg-emerald-500" : pct >= 40 ? "bg-amber-400" : "bg-red-400";
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-1.5 bg-slate-100 rounded-full overflow-hidden">
        <div className={cn("h-full rounded-full transition-all", color)} style={{ width: `${pct}%` }} />
      </div>
      <span className="text-xs tabular-nums text-slate-600 w-10 text-right">{pct}%</span>
    </div>
  );
}

const CRITICALITY_COLORS: Record<string, string> = {
  critical: "text-red-600 bg-red-50",
  major: "text-orange-600 bg-orange-50",
  minor: "text-amber-600 bg-amber-50",
  info: "text-slate-600 bg-slate-100",
};

export default function CoveragePage() {
  const { id } = useParams<{ id: string }>();
  const [coverage, setCoverage] = useState<ReferentialCoverage | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeSection, setActiveSection] = useState<string | null>(null);
  const [showUncoveredOnly, setShowUncoveredOnly] = useState(false);

  useEffect(() => {
    controlsApi
      .getCoverage(id)
      .then((data) => {
        setCoverage(data);
        if (data.sections.length > 0) setActiveSection(data.sections[0].sectionId);
      })
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="p-8 text-slate-400">Chargement de l'analyse...</div>;
  if (!coverage) return <div className="p-8 text-red-500">Référentiel introuvable</div>;

  const visibleQuestions = coverage.questions.filter((q) => {
    if (activeSection && q.sectionId !== activeSection) return false;
    if (showUncoveredOnly && q.controls.length > 0) return false;
    return true;
  });

  const pct = coverage.coveragePercent;
  const coverageColor = pct >= 75 ? "text-emerald-600" : pct >= 40 ? "text-amber-600" : "text-red-500";

  return (
    <div className="p-8 max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Link href="/auditor/referentials" className="text-slate-400 hover:text-slate-600">
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div>
          <h1 className="text-xl font-bold text-slate-900">
            Couverture — {coverage.referentialName}
          </h1>
          <p className="text-sm text-slate-500">{coverage.referentialCode}</p>
        </div>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-4 gap-4">
        <div className="bg-white border border-slate-200 rounded-2xl p-5">
          <p className="text-xs text-slate-500 font-medium mb-1">Questions totales</p>
          <p className="text-3xl font-bold text-slate-900">{coverage.totalQuestions}</p>
        </div>
        <div className="bg-white border border-slate-200 rounded-2xl p-5">
          <p className="text-xs text-slate-500 font-medium mb-1">Couvertes</p>
          <p className="text-3xl font-bold text-emerald-600">{coverage.coveredQuestions}</p>
        </div>
        <div className="bg-white border border-slate-200 rounded-2xl p-5">
          <p className="text-xs text-slate-500 font-medium mb-1">Non couvertes</p>
          <p className="text-3xl font-bold text-red-500">
            {coverage.totalQuestions - coverage.coveredQuestions}
          </p>
        </div>
        <div className="bg-white border border-slate-200 rounded-2xl p-5">
          <p className="text-xs text-slate-500 font-medium mb-1">Couverture globale</p>
          <p className={cn("text-3xl font-bold", coverageColor)}>{pct}%</p>
          <div className="mt-2">
            <CoverageBar pct={pct} />
          </div>
        </div>
      </div>

      <div className="grid grid-cols-4 gap-6">
        {/* Section sidebar */}
        <div className="col-span-1 space-y-1">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide px-2 mb-2">Sections</p>
          {coverage.sections.map((s) => (
            <button
              key={s.sectionId}
              onClick={() => setActiveSection(s.sectionId)}
              className={cn(
                "w-full text-left px-3 py-2.5 rounded-xl text-sm transition-colors",
                activeSection === s.sectionId
                  ? "bg-blue-50 text-blue-700 font-medium"
                  : "text-slate-600 hover:bg-slate-50"
              )}
            >
              <p className="truncate">{s.sectionTitle}</p>
              <CoverageBar pct={s.coveragePercent} />
            </button>
          ))}
        </div>

        {/* Question list */}
        <div className="col-span-3">
          <div className="flex items-center justify-between mb-3">
            <p className="text-sm font-semibold text-slate-700">
              {visibleQuestions.length} question{visibleQuestions.length !== 1 ? "s" : ""}
            </p>
            <label className="flex items-center gap-2 text-xs text-slate-600 cursor-pointer">
              <input
                type="checkbox"
                checked={showUncoveredOnly}
                onChange={(e) => setShowUncoveredOnly(e.target.checked)}
                className="rounded"
              />
              Afficher uniquement les non couvertes
            </label>
          </div>

          <div className="space-y-2">
            {visibleQuestions.map((q) => (
              <QuestionRow key={q.questionId} question={q} />
            ))}
            {visibleQuestions.length === 0 && (
              <div className="text-center py-10">
                <CheckCircle2 className="h-8 w-8 text-emerald-400 mx-auto mb-2" />
                <p className="text-sm text-slate-500">
                  {showUncoveredOnly
                    ? "Toutes les questions de cette section sont couvertes."
                    : "Aucune question dans cette section."}
                </p>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function QuestionRow({ question }: { question: QuestionCoverage }) {
  const covered = question.controls.length > 0;
  return (
    <div
      className={cn(
        "bg-white border rounded-xl p-4 transition-colors",
        covered ? "border-slate-200" : "border-amber-200 bg-amber-50/30"
      )}
    >
      <div className="flex items-start gap-3">
        <div className="mt-0.5 shrink-0">
          {covered ? (
            <CheckCircle2 className="h-4 w-4 text-emerald-500" />
          ) : (
            <Circle className="h-4 w-4 text-amber-400" />
          )}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            {question.questionCode && (
              <span className="font-mono text-xs text-slate-500">{question.questionCode}</span>
            )}
            <span
              className={cn(
                "text-[10px] font-semibold px-1.5 py-0.5 rounded-full",
                CRITICALITY_COLORS[question.criticality] ?? "bg-slate-100 text-slate-600"
              )}
            >
              {question.criticality}
            </span>
          </div>
          <p className="text-sm text-slate-700 line-clamp-2">{question.questionText}</p>
          {question.controls.length > 0 && (
            <div className="flex flex-wrap gap-1.5 mt-2">
              {question.controls.map((c) => (
                <Link
                  key={c.controlId}
                  href={`/auditor/controls/${c.controlId}`}
                  className="flex items-center gap-1 text-xs font-medium text-blue-700 bg-blue-50 px-2 py-0.5 rounded-full hover:bg-blue-100 transition-colors"
                >
                  <Shield className="h-2.5 w-2.5" />
                  {c.code}
                </Link>
              ))}
            </div>
          )}
          {!covered && (
            <p className="text-xs text-amber-600 mt-1.5 flex items-center gap-1">
              <AlertCircle className="h-3 w-3" />
              Aucun contrôle ne couvre cette question
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
