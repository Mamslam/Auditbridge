"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import {
  ClipboardCheck, CheckCircle2, AlertTriangle, Loader2, Send,
  ChevronDown, ChevronRight, Bot, Paperclip
} from "lucide-react";
import { auditsApi } from "@/lib/api/audits";
import type { AuditDetail, AuditQuestion, AnswerType } from "@/lib/types";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

export default function ClientAuditPortalPage() {
  const { token } = useParams<{ token: string }>();
  const [audit, setAudit] = useState<AuditDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set());
  const [saving, setSaving] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);

  useEffect(() => {
    auditsApi.getByToken(token)
      .then((data) => {
        setAudit(data);
        if (data.status === "submitted" || data.status === "completed") {
          setSubmitted(true);
        }
        if (data.sections?.length) {
          setExpandedSections(new Set([data.sections[0].id]));
        }
      })
      .catch(() => setAudit(null))
      .finally(() => setLoading(false));
  }, [token]);

  const getResponse = (questionId: string) =>
    audit?.responses.find((r) => r.questionId === questionId);

  const handleAnswer = async (questionId: string, answerValue: string, answerNotes?: string) => {
    setSaving(questionId);
    try {
      await auditsApi.upsertResponseByToken(token, { questionId, answerValue, answerNotes });
      setAudit((prev) => {
        if (!prev) return prev;
        const existing = prev.responses.find((r) => r.questionId === questionId);
        if (existing) {
          return { ...prev, responses: prev.responses.map((r) =>
            r.questionId === questionId ? { ...r, answerValue, answerNotes } : r) };
        }
        return { ...prev, responses: [...prev.responses, {
          id: crypto.randomUUID(), auditId: prev.id, questionId,
          answerValue, answerNotes, isFlagged: false, updatedAt: new Date().toISOString(),
        }]};
      });
    } catch {
      toast.error("Erreur lors de la sauvegarde");
    } finally {
      setSaving(null);
    }
  };

  const handleSubmit = async () => {
    setSubmitting(true);
    try {
      await auditsApi.submitByToken(token);
      setSubmitted(true);
      toast.success("Audit soumis avec succès !");
    } catch {
      toast.error("Erreur lors de la soumission");
    } finally {
      setSubmitting(false);
    }
  };

  const allQuestions = (audit?.sections ?? []).flatMap((s) => s.questions);
  const answered = allQuestions.filter((q) => getResponse(q.id)?.answerValue).length;
  const total = allQuestions.length;
  const progress = total > 0 ? Math.round((answered / total) * 100) : 0;

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
      </div>
    );
  }

  if (!audit) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
        <div className="text-center">
          <AlertTriangle className="h-10 w-10 text-amber-400 mx-auto mb-3" />
          <h2 className="font-semibold text-slate-900">Lien invalide ou expiré</h2>
          <p className="text-sm text-slate-500 mt-1">Ce lien d'audit n'est pas valide ou a expiré.</p>
        </div>
      </div>
    );
  }

  if (submitted) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
        <div className="text-center max-w-md">
          <div className="h-16 w-16 rounded-2xl bg-emerald-100 flex items-center justify-center mx-auto mb-4">
            <CheckCircle2 className="h-8 w-8 text-emerald-600" />
          </div>
          <h2 className="text-xl font-bold text-slate-900 mb-2">Audit soumis avec succès</h2>
          <p className="text-slate-500 text-sm">
            Vos réponses ont été transmises à l'auditeur. Vous recevrez un rapport une fois l'analyse terminée.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <div className="bg-white border-b border-slate-200 sticky top-0 z-10">
        <div className="max-w-3xl mx-auto px-4 sm:px-6 py-4">
          <div className="flex items-center justify-between gap-4">
            <div className="flex items-center gap-2.5 min-w-0">
              <div className="h-7 w-7 rounded-lg bg-blue-600 flex items-center justify-center shrink-0">
                <ClipboardCheck className="h-3.5 w-3.5 text-white" />
              </div>
              <div className="min-w-0">
                <p className="font-semibold text-slate-900 text-sm truncate">{audit.title}</p>
                <p className="text-xs text-slate-400">{audit.referential?.name ?? "—"}</p>
              </div>
            </div>
            <div className="flex items-center gap-3 shrink-0">
              <div className="hidden sm:flex flex-col items-end">
                <p className="text-xs text-slate-400">{answered}/{total} questions</p>
                <div className="w-24 h-1.5 bg-slate-100 rounded-full mt-1">
                  <div className="h-1.5 bg-blue-500 rounded-full transition-all" style={{ width: `${progress}%` }} />
                </div>
              </div>
              <button
                onClick={handleSubmit}
                disabled={submitting || answered === 0}
                className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-xl text-sm font-semibold hover:bg-blue-700 disabled:opacity-50 transition-colors"
              >
                {submitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                Soumettre
              </button>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-3xl mx-auto px-4 sm:px-6 py-8 space-y-4">
        {audit.scope && (
          <div className="bg-blue-50 border border-blue-100 rounded-2xl p-4">
            <p className="text-sm font-semibold text-blue-900 mb-1">Périmètre de l'audit</p>
            <p className="text-sm text-blue-700">{audit.scope}</p>
          </div>
        )}

        {(audit.sections ?? []).map((section, si) => {
          const expanded = expandedSections.has(section.id);
          const sectionAnswered = section.questions.filter((q) => getResponse(q.id)?.answerValue).length;
          return (
            <div key={section.id} className="bg-white rounded-2xl border border-slate-200 overflow-hidden">
              <button
                onClick={() => setExpandedSections((prev) => {
                  const next = new Set(prev);
                  expanded ? next.delete(section.id) : next.add(section.id);
                  return next;
                })}
                className="w-full flex items-center gap-3 px-5 py-4 text-left hover:bg-slate-50 transition-colors"
              >
                {expanded ? <ChevronDown className="h-4 w-4 text-slate-400 shrink-0" /> : <ChevronRight className="h-4 w-4 text-slate-400 shrink-0" />}
                <div className="flex-1 min-w-0">
                  <span className="text-sm font-semibold text-slate-900">{si + 1}. {section.title}</span>
                </div>
                <span className={cn(
                  "text-xs font-medium px-2 py-0.5 rounded-full shrink-0",
                  sectionAnswered === section.questions.length ? "text-emerald-700 bg-emerald-100" : "text-slate-500 bg-slate-100"
                )}>
                  {sectionAnswered}/{section.questions.length}
                </span>
              </button>

              {expanded && (
                <div className="divide-y divide-slate-100 border-t border-slate-100">
                  {section.questions.map((question, qi) => (
                    <QuestionItem
                      key={question.id}
                      index={qi + 1}
                      question={question}
                      response={getResponse(question.id)}
                      saving={saving === question.id}
                      onAnswer={handleAnswer}
                    />
                  ))}
                </div>
              )}
            </div>
          );
        })}

        <div className="bg-white rounded-2xl border border-slate-200 p-5 flex items-center justify-between gap-4">
          <div>
            <p className="font-semibold text-slate-900 text-sm">{answered}/{total} questions répondues</p>
            <p className="text-xs text-slate-400 mt-0.5">
              {answered < total ? `${total - answered} question(s) restante(s)` : "Toutes les questions ont été répondues"}
            </p>
          </div>
          <button
            onClick={handleSubmit}
            disabled={submitting || answered === 0}
            className="flex items-center gap-2 bg-blue-600 text-white px-5 py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-700 disabled:opacity-50 transition-colors shrink-0"
          >
            {submitting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
            {submitting ? "Soumission..." : "Soumettre l'audit"}
          </button>
        </div>
      </div>
    </div>
  );
}

function QuestionItem({
  index, question, response, saving, onAnswer,
}: {
  index: number;
  question: AuditQuestion;
  response: { answerValue?: string; answerNotes?: string; aiAnalysis?: string } | undefined;
  saving: boolean;
  onAnswer: (questionId: string, value: string, notes?: string) => void;
}) {
  const [notes, setNotes] = useState(response?.answerNotes ?? "");
  const answerType: AnswerType = question.answerType ?? "yes_no";

  const critColor: Record<string, string> = {
    critical: "border-l-4 border-red-400",
    major: "border-l-4 border-orange-400",
    minor: "border-l-4 border-amber-300",
    info: "",
  };

  return (
    <div className={cn("px-5 py-5", critColor[question.criticality] ?? "")}>
      <div className="flex items-start gap-2 mb-3">
        <span className="text-xs text-slate-400 mt-0.5 shrink-0 w-5">{index}.</span>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-slate-900 leading-snug">{question.text}</p>
          {question.guidance && (
            <p className="text-xs text-slate-400 mt-1 leading-relaxed">{question.guidance}</p>
          )}
        </div>
        <div className="flex items-center gap-1.5 shrink-0">
          {saving && <Loader2 className="h-3.5 w-3.5 animate-spin text-slate-400" />}
          {response?.answerValue && !saving && <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500" />}
          {question.isMandatory && <span className="text-[10px] text-red-500 font-medium">Requis</span>}
        </div>
      </div>

      {answerType === "yes_no" && (
        <div className="flex gap-2 ml-7">
          {(["Oui", "Non", "N/A"] as const).map((val) => (
            <button key={val} onClick={() => onAnswer(question.id, val, notes)}
              className={cn(
                "px-4 py-2 rounded-xl text-sm font-medium border transition-colors",
                response?.answerValue === val
                  ? val === "Oui" ? "bg-emerald-600 text-white border-emerald-600"
                  : val === "Non" ? "bg-red-600 text-white border-red-600"
                  : "bg-slate-600 text-white border-slate-600"
                  : "bg-white text-slate-600 border-slate-200 hover:border-slate-300"
              )}
            >{val}</button>
          ))}
        </div>
      )}

      {answerType === "rating_1_5" && (
        <div className="flex gap-2 ml-7">
          {[1, 2, 3, 4, 5].map((n) => (
            <button key={n} onClick={() => onAnswer(question.id, String(n), notes)}
              className={cn(
                "h-9 w-9 rounded-xl text-sm font-bold border transition-colors",
                response?.answerValue === String(n)
                  ? "bg-blue-600 text-white border-blue-600"
                  : "bg-white text-slate-600 border-slate-200 hover:border-blue-300"
              )}
            >{n}</button>
          ))}
        </div>
      )}

      {(answerType === "text" || answerType === "numeric") && (
        <div className="ml-7">
          <input type={answerType === "numeric" ? "number" : "text"}
            defaultValue={response?.answerValue ?? ""}
            onBlur={(e) => e.target.value && onAnswer(question.id, e.target.value, notes)}
            placeholder={answerType === "numeric" ? "Valeur numérique..." : "Votre réponse..."}
            className="w-full text-sm border border-slate-200 rounded-xl px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      )}

      {answerType === "file_upload" && (
        <div className="ml-7">
          <label className="flex items-center gap-2 px-4 py-2.5 border border-dashed border-slate-300 rounded-xl text-sm text-slate-500 cursor-pointer hover:border-blue-400 hover:text-blue-600 transition-colors w-fit">
            <Paperclip className="h-4 w-4" />
            Joindre un fichier
            <input type="file" className="hidden" onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) onAnswer(question.id, `[Fichier: ${file.name}]`, notes);
            }} />
          </label>
          {response?.answerValue && <p className="text-xs text-emerald-600 mt-1.5">{response.answerValue}</p>}
        </div>
      )}

      {response?.aiAnalysis && (
        <div className="ml-7 mt-3 flex items-start gap-2 bg-purple-50 rounded-xl p-3 border border-purple-100">
          <Bot className="h-3.5 w-3.5 text-purple-600 mt-0.5 shrink-0" />
          <p className="text-xs text-purple-800">{response.aiAnalysis}</p>
        </div>
      )}

      {response?.answerValue && (
        <div className="ml-7 mt-3">
          <textarea value={notes} onChange={(e) => setNotes(e.target.value)}
            onBlur={() => response.answerValue && onAnswer(question.id, response.answerValue, notes)}
            placeholder="Commentaire ou justification (optionnel)..."
            rows={2}
            className="w-full text-xs text-slate-600 border border-slate-200 rounded-xl px-3 py-2 focus:outline-none focus:ring-1 focus:ring-blue-400 resize-none placeholder:text-slate-300"
          />
        </div>
      )}
    </div>
  );
}
