"use client";

import { useEffect, useState, useCallback, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import {
  Plus,
  Trash2,
  GripVertical,
  ChevronDown,
  ChevronRight,
  Save,
  ArrowLeft,
  AlertCircle,
  FileCheck,
  Loader2,
} from "lucide-react";
import Link from "next/link";
import { referentialsApi } from "@/lib/api/referentials";
import { api } from "@/lib/api/client";
import type { ReferentialDetail, TemplateSection, TemplateQuestion, AnswerType, Criticality } from "@/lib/types";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

const ANSWER_TYPE_LABELS: Record<AnswerType, string> = {
  yes_no: "Oui / Non",
  rating_1_5: "Note 1-5",
  text: "Texte libre",
  file_upload: "Fichier",
  multi_select: "Choix multiple",
  numeric: "Numérique",
};

const CRITICALITY_CONFIG: Record<Criticality, { label: string; color: string }> = {
  info:     { label: "Info",     color: "text-slate-600 bg-slate-100" },
  minor:    { label: "Mineur",   color: "text-amber-700 bg-amber-100" },
  major:    { label: "Majeur",   color: "text-orange-700 bg-orange-100" },
  critical: { label: "Critique", color: "text-red-700 bg-red-100" },
};

function TemplateEditorInner() {
  const searchParams = useSearchParams();
  const refId = searchParams.get("ref");

  const [referential, setReferential] = useState<ReferentialDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set());
  const [activeQuestion, setActiveQuestion] = useState<string | null>(null);
  const [editingSection, setEditingSection] = useState<string | null>(null);

  useEffect(() => {
    if (!refId) { setLoading(false); return; }
    referentialsApi.getById(refId)
      .then((data) => {
        // Backend returns `question` field; frontend type uses `text`
        const mapped = {
          ...data,
          sections: data.sections.map((s) => ({
            ...s,
            questions: (s.questions ?? []).map((q: TemplateQuestion) => ({
              ...q,
              text: (q as unknown as Record<string, unknown>).question as string ?? q.text,
            })) as TemplateQuestion[],
          })),
        };
        setReferential(mapped);
        setExpandedSections(new Set(data.sections.map((s) => s.id)));
      })
      .finally(() => setLoading(false));
  }, [refId]);

  const addSection = async () => {
    if (!refId) return;
    const title = `Nouvelle section ${(referential?.sections.length ?? 0) + 1}`;
    try {
      const section = await api.post<TemplateSection>(`/api/referentials/${refId}/sections`, {
        title,
        orderIndex: (referential?.sections.length ?? 0),
      });
      setReferential((prev) => prev ? { ...prev, sections: [...prev.sections, { ...section, questions: [] }] } : prev);
      setExpandedSections((prev) => new Set([...prev, section.id]));
    } catch {
      toast.error("Erreur lors de l'ajout de section");
    }
  };

  const deleteSection = async (sectionId: string) => {
    try {
      await api.delete(`/api/referentials/${refId}/sections/${sectionId}`);
      setReferential((prev) => prev
        ? { ...prev, sections: prev.sections.filter((s) => s.id !== sectionId) }
        : prev
      );
    } catch {
      toast.error("Erreur lors de la suppression");
    }
  };

  const addQuestion = async (sectionId: string) => {
    const raw = await api.post<Record<string, unknown>>(`/api/referentials/${refId}/sections/${sectionId}/questions`, {
      question: "Nouvelle question",
      answerType: "yes_no",
      criticality: "minor",
      isMandatory: true,
      orderIndex: (referential?.sections.find((s) => s.id === sectionId)?.questions?.length ?? 0),
    });
    const q: TemplateQuestion = { ...raw, text: raw.question as string } as unknown as TemplateQuestion;
    setReferential((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        sections: prev.sections.map((s) =>
          s.id === sectionId
            ? { ...s, questions: [...(s.questions ?? []), q] }
            : s
        ),
      };
    });
    setActiveQuestion(q.id);
  };

  const updateQuestion = useCallback((sectionId: string, questionId: string, patch: Partial<TemplateQuestion>) => {
    setReferential((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        sections: prev.sections.map((s) =>
          s.id === sectionId
            ? {
                ...s,
                questions: s.questions?.map((q) =>
                  q.id === questionId ? { ...q, ...patch } : q
                ),
              }
            : s
        ),
      };
    });
  }, []);

  const saveQuestion = async (sectionId: string, question: TemplateQuestion) => {
    setSaving(true);
    try {
      await api.put(`/api/referentials/${refId}/questions/${question.id}`, {
        question: question.text,
        guidance: question.guidance ?? null,
        answerType: question.answerType,
        criticality: question.criticality,
        isMandatory: question.isMandatory,
        orderIndex: question.orderIndex,
        code: null,
        expectedEvidence: question.expectedEvidence ?? null,
        tags: question.tags ?? null,
      });
      toast.success("Question sauvegardée");
    } catch {
      toast.error("Erreur lors de la sauvegarde");
    } finally {
      setSaving(false);
    }
  };

  const deleteQuestion = async (sectionId: string, questionId: string) => {
    await api.delete(`/api/referentials/${refId}/questions/${questionId}`);
    setReferential((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        sections: prev.sections.map((s) =>
          s.id === sectionId
            ? { ...s, questions: s.questions?.filter((q) => q.id !== questionId) }
            : s
        ),
      };
    });
    if (activeQuestion === questionId) setActiveQuestion(null);
  };

  const totalQuestions = referential?.sections.reduce((sum, s) => sum + (s.questions?.length ?? 0), 0) ?? 0;

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
      </div>
    );
  }

  if (!refId || !referential) {
    return (
      <div className="p-8 max-w-2xl mx-auto text-center">
        <FileCheck className="h-12 w-12 text-slate-300 mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-slate-900 mb-2">Éditeur de template</h2>
        <p className="text-slate-500 text-sm mb-6">
          Sélectionnez un référentiel depuis la liste pour commencer à éditer ses sections et questions.
        </p>
        <Link
          href="/auditor/referentials"
          className="inline-flex items-center gap-2 text-sm font-medium text-blue-600 hover:text-blue-700"
        >
          <ArrowLeft className="h-4 w-4" /> Aller aux référentiels
        </Link>
      </div>
    );
  }

  const selectedQuestion = activeQuestion
    ? referential.sections
        .flatMap((s) => (s.questions ?? []).map((q) => ({ ...q, sectionId: s.id })))
        .find((q) => q.id === activeQuestion)
    : null;

  return (
    <div className="flex h-full">
      {/* Left panel — sections + questions */}
      <div className="flex-1 overflow-y-auto p-8 pr-4">
        {/* Header */}
        <div className="flex items-center gap-3 mb-6">
          <Link href="/auditor/referentials" className="text-slate-400 hover:text-slate-600 transition-colors">
            <ArrowLeft className="h-5 w-5" />
          </Link>
          <div className="flex-1 min-w-0">
            <h1 className="text-xl font-bold text-slate-900 truncate">{referential.name}</h1>
            <p className="text-xs text-slate-400 mt-0.5">
              {referential.sections.length} sections · {totalQuestions} questions
            </p>
          </div>
          <button
            onClick={addSection}
            className="flex items-center gap-1.5 bg-blue-600 text-white px-3.5 py-2 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-colors shrink-0"
          >
            <Plus className="h-4 w-4" /> Section
          </button>
        </div>

        {/* Sections */}
        <div className="space-y-3">
          {referential.sections.map((section, si) => {
            const expanded = expandedSections.has(section.id);
            return (
              <div key={section.id} className="bg-white rounded-2xl border border-slate-200 overflow-hidden">
                {/* Section header */}
                <div className="flex items-center gap-3 px-4 py-3 border-b border-slate-100 group">
                  <GripVertical className="h-4 w-4 text-slate-300 cursor-grab shrink-0" />
                  <button
                    onClick={() => setExpandedSections((prev) => {
                      const next = new Set(prev);
                      expanded ? next.delete(section.id) : next.add(section.id);
                      return next;
                    })}
                    className="flex items-center gap-2 flex-1 min-w-0 text-left"
                  >
                    {expanded ? (
                      <ChevronDown className="h-4 w-4 text-slate-400 shrink-0" />
                    ) : (
                      <ChevronRight className="h-4 w-4 text-slate-400 shrink-0" />
                    )}
                    {editingSection === section.id ? (
                      <input
                        autoFocus
                        defaultValue={section.title}
                        onBlur={async (e) => {
                          await api.put(`/api/referentials/${refId}/sections/${section.id}`, { title: e.target.value });
                          setReferential((prev) => prev ? {
                            ...prev,
                            sections: prev.sections.map((s) => s.id === section.id ? { ...s, title: e.target.value } : s),
                          } : prev);
                          setEditingSection(null);
                        }}
                        onKeyDown={(e) => e.key === "Enter" && (e.target as HTMLInputElement).blur()}
                        className="flex-1 text-sm font-semibold text-slate-900 bg-transparent outline-none border-b border-blue-400"
                        onClick={(e) => e.stopPropagation()}
                      />
                    ) : (
                      <span
                        className="text-sm font-semibold text-slate-900 flex-1 truncate"
                        onDoubleClick={() => setEditingSection(section.id)}
                      >
                        {si + 1}. {section.title}
                      </span>
                    )}
                    <span className="text-xs text-slate-400 shrink-0">
                      {section.questions?.length ?? 0} q.
                    </span>
                  </button>
                  <button
                    onClick={() => deleteSection(section.id)}
                    className="opacity-0 group-hover:opacity-100 text-slate-300 hover:text-red-500 transition-all p-1 rounded"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </div>

                {/* Questions */}
                {expanded && (
                  <div className="divide-y divide-slate-50">
                    {(section.questions ?? []).map((q, qi) => {
                      const crit = CRITICALITY_CONFIG[q.criticality];
                      const isActive = activeQuestion === q.id;
                      return (
                        <div
                          key={q.id}
                          onClick={() => setActiveQuestion(isActive ? null : q.id)}
                          className={cn(
                            "flex items-start gap-3 px-4 py-3 cursor-pointer group transition-colors",
                            isActive ? "bg-blue-50" : "hover:bg-slate-50"
                          )}
                        >
                          <GripVertical className="h-4 w-4 text-slate-300 cursor-grab mt-0.5 shrink-0" />
                          <div className="flex-1 min-w-0">
                            <p className="text-sm text-slate-800 leading-snug">
                              <span className="text-slate-400 text-xs mr-1">{qi + 1}.</span>
                              {q.text}
                            </p>
                            <div className="flex items-center gap-2 mt-1.5">
                              <span className={cn("text-[10px] font-medium px-1.5 py-0.5 rounded-full", crit.color)}>
                                {crit.label}
                              </span>
                              <span className="text-[10px] text-slate-400">
                                {ANSWER_TYPE_LABELS[q.answerType]}
                              </span>
                              {q.isMandatory && (
                                <span className="text-[10px] text-red-500">Obligatoire</span>
                              )}
                            </div>
                          </div>
                          <button
                            onClick={(e) => { e.stopPropagation(); deleteQuestion(section.id, q.id); }}
                            className="opacity-0 group-hover:opacity-100 text-slate-300 hover:text-red-500 transition-all p-1 rounded mt-0.5 shrink-0"
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </button>
                        </div>
                      );
                    })}

                    <div className="px-4 py-2.5">
                      <button
                        onClick={() => addQuestion(section.id)}
                        className="flex items-center gap-1.5 text-xs text-slate-400 hover:text-blue-600 transition-colors"
                      >
                        <Plus className="h-3.5 w-3.5" />
                        Ajouter une question
                      </button>
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* Right panel — question editor */}
      <div className={cn(
        "w-96 shrink-0 border-l border-slate-200 bg-white overflow-y-auto transition-all",
        !selectedQuestion && "hidden lg:flex lg:items-center lg:justify-center"
      )}>
        {selectedQuestion ? (
          <QuestionEditor
            question={selectedQuestion}
            onChange={(patch) => updateQuestion(selectedQuestion.sectionId, selectedQuestion.id, patch)}
            onSave={() => saveQuestion(selectedQuestion.sectionId, selectedQuestion as TemplateQuestion)}
            saving={saving}
          />
        ) : (
          <div className="p-8 text-center">
            <AlertCircle className="h-8 w-8 text-slate-300 mx-auto mb-3" />
            <p className="text-sm text-slate-400">Cliquez sur une question pour l'éditer</p>
          </div>
        )}
      </div>
    </div>
  );
}

function QuestionEditor({
  question,
  onChange,
  onSave,
  saving,
}: {
  question: TemplateQuestion & { sectionId: string };
  onChange: (patch: Partial<TemplateQuestion>) => void;
  onSave: () => void;
  saving: boolean;
}) {
  return (
    <div className="p-6 space-y-5">
      <div className="flex items-center justify-between">
        <h3 className="font-semibold text-slate-900 text-sm">Éditer la question</h3>
        <button
          onClick={onSave}
          disabled={saving}
          className="flex items-center gap-1.5 bg-blue-600 text-white px-3 py-1.5 rounded-lg text-xs font-semibold hover:bg-blue-700 disabled:opacity-50 transition-colors"
        >
          {saving ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Save className="h-3.5 w-3.5" />}
          Enregistrer
        </button>
      </div>

      <Field label="Texte de la question">
        <textarea
          value={question.text}
          onChange={(e) => onChange({ text: e.target.value })}
          rows={3}
          className="w-full text-sm border border-slate-200 rounded-xl p-3 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
        />
      </Field>

      <Field label="Guidance (optionnelle)">
        <textarea
          value={question.guidance ?? ""}
          onChange={(e) => onChange({ guidance: e.target.value })}
          rows={2}
          placeholder="Aide à l'interprétation..."
          className="w-full text-sm border border-slate-200 rounded-xl p-3 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
        />
      </Field>

      <Field label="Référence réglementaire">
        <input
          type="text"
          value={question.regulatoryRef ?? ""}
          onChange={(e) => onChange({ regulatoryRef: e.target.value })}
          placeholder="ex: Article 4.1 / CH6.2"
          className="w-full text-sm border border-slate-200 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </Field>

      <div className="grid grid-cols-2 gap-3">
        <Field label="Type de réponse">
          <select
            value={question.answerType}
            onChange={(e) => onChange({ answerType: e.target.value as AnswerType })}
            className="w-full text-sm border border-slate-200 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {Object.entries(ANSWER_TYPE_LABELS).map(([k, v]) => (
              <option key={k} value={k}>{v}</option>
            ))}
          </select>
        </Field>

        <Field label="Criticité">
          <select
            value={question.criticality}
            onChange={(e) => onChange({ criticality: e.target.value as Criticality })}
            className="w-full text-sm border border-slate-200 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {Object.entries(CRITICALITY_CONFIG).map(([k, v]) => (
              <option key={k} value={k}>{v.label}</option>
            ))}
          </select>
        </Field>
      </div>

      <Field label="Tags (séparés par virgule)">
        <input
          type="text"
          value={(question.tags ?? []).join(", ")}
          onChange={(e) =>
            onChange({ tags: e.target.value.split(",").map((t) => t.trim()).filter(Boolean) })
          }
          placeholder="ex: documentation, formation"
          className="w-full text-sm border border-slate-200 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </Field>

      <label className="flex items-center gap-2.5 cursor-pointer">
        <input
          type="checkbox"
          checked={question.isMandatory}
          onChange={(e) => onChange({ isMandatory: e.target.checked })}
          className="h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
        />
        <span className="text-sm text-slate-700">Question obligatoire</span>
      </label>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label className="text-xs font-medium text-slate-700">{label}</label>
      {children}
    </div>
  );
}

export default function TemplatesPage() {
  return (
    <Suspense fallback={
      <div className="flex items-center justify-center h-full">
        <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
      </div>
    }>
      <TemplateEditorInner />
    </Suspense>
  );
}
