"use client";

import React, { useEffect, useState, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import { auditsApi } from "@/lib/api/audits";
import type { AuditDetail, AuditQuestion, AuditSection, ConformityRating } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  CheckCircle2, AlertTriangle, AlertCircle, XCircle, Minus,
  ChevronLeft, ChevronRight, Navigation, Loader2, Camera, Upload,
  CheckCheck,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

// ── Types ─────────────────────────────────────────────────────────────────────

const CONFORMITY: Array<{
  value: ConformityRating;
  label: string;
  color: string;
  icon: React.ReactNode;
}> = [
  { value: "compliant", label: "Conforme",  color: "border-emerald-500 bg-emerald-50 text-emerald-700", icon: <CheckCircle2 className="h-5 w-5" /> },
  { value: "minor",     label: "Mineur",    color: "border-amber-400 bg-amber-50 text-amber-700",       icon: <AlertTriangle className="h-5 w-5" /> },
  { value: "major",     label: "Majeur",    color: "border-orange-500 bg-orange-50 text-orange-700",    icon: <AlertCircle className="h-5 w-5" /> },
  { value: "critical",  label: "Critique",  color: "border-red-600 bg-red-50 text-red-700",             icon: <XCircle className="h-5 w-5" /> },
  { value: "na",        label: "N/A",       color: "border-slate-400 bg-slate-50 text-slate-600",       icon: <Minus className="h-5 w-5" /> },
];

// ── Flatten questions ─────────────────────────────────────────────────────────

function flattenQuestions(sections: AuditSection[]) {
  const result: Array<{ question: AuditQuestion; sectionTitle: string; sectionIndex: number }> = [];
  sections.forEach((sec, si) => {
    sec.questions.forEach((q) => {
      result.push({ question: q, sectionTitle: sec.title, sectionIndex: si });
    });
  });
  return result;
}

// ── Main ─────────────────────────────────────────────────────────────────────

export default function InspectPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();

  const [audit, setAudit] = useState<AuditDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [index, setIndex] = useState(0);
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [uploadingPhoto, setUploadingPhoto] = useState(false);
  const [gpsLoading, setGpsLoading] = useState(false);
  const [done, setDone] = useState(false);

  const load = useCallback(async () => {
    try {
      const a = await auditsApi.getById(id);
      setAudit(a);
    } catch {
      toast.error("Impossible de charger l'audit");
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  const questions = audit ? flattenQuestions(audit.sections) : [];
  const total = questions.length;
  const current = questions[index];
  const response = audit?.responses.find(r => r.questionId === current?.question.id);
  const answered = audit?.responses.filter(r => r.conformity && r.conformity !== "pending").length ?? 0;

  async function handleConformity(conformity: ConformityRating) {
    if (!audit || !current) return;
    setSubmitting(true);
    try {
      const existing = audit.responses.find(r => r.questionId === current.question.id);
      if (!existing) {
        // Create response first
        await auditsApi.upsertResponse(audit.id, {
          questionId: current.question.id,
          answerNotes: notes || undefined,
        });
        const refreshed = await auditsApi.getById(audit.id);
        const newResp = refreshed.responses.find(r => r.questionId === current.question.id);
        if (newResp) {
          await auditsApi.setConformity(audit.id, newResp.id, { conformity, auditorComment: notes || undefined });
        }
      } else {
        if (notes) {
          await auditsApi.upsertResponse(audit.id, {
            questionId: current.question.id,
            answerNotes: notes,
          });
        }
        await auditsApi.setConformity(audit.id, existing.id, { conformity, auditorComment: notes || undefined });
      }
      await load();
      setNotes("");
      // Auto-advance
      if (index < total - 1) {
        setIndex(i => i + 1);
      } else {
        setDone(true);
      }
      toast.success("Réponse enregistrée");
    } catch {
      toast.error("Erreur lors de l'enregistrement");
    } finally {
      setSubmitting(false);
    }
  }

  async function handlePhotoUpload(file: File) {
    if (!audit || !current) return;
    setUploadingPhoto(true);
    try {
      const { signedUrl, storagePath } = await auditsApi.presignUpload(audit.id, file.name);
      await fetch(signedUrl, { method: "PUT", body: file, headers: { "Content-Type": file.type } });
      await auditsApi.registerEvidence(audit.id, {
        fileName: file.name,
        storagePath,
        fileSizeBytes: file.size,
        mimeType: file.type || "image/jpeg",
        responseId: response?.id,
      });
      toast.success("Photo ajoutée");
    } catch {
      toast.error("Échec de l'upload photo");
    } finally {
      setUploadingPhoto(false);
    }
  }

  function handleGps() {
    if (!navigator.geolocation) { toast.error("GPS non disponible"); return; }
    setGpsLoading(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setNotes(n => n + (n ? "\n" : "") + `📍 ${pos.coords.latitude.toFixed(6)}, ${pos.coords.longitude.toFixed(6)}`);
        setGpsLoading(false);
        toast.success("Position ajoutée aux notes");
      },
      () => { toast.error("GPS indisponible"); setGpsLoading(false); }
    );
  }

  if (loading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-slate-400" />
      </div>
    );
  }

  if (!audit || total === 0) {
    return (
      <div className="flex h-screen flex-col items-center justify-center gap-4 p-6">
        <p className="text-slate-500">Aucune question dans cet audit.</p>
        <Button variant="outline" onClick={() => router.back()}>Retour</Button>
      </div>
    );
  }

  if (done) {
    return (
      <div className="flex h-screen flex-col items-center justify-center gap-4 p-6 text-center">
        <CheckCheck className="h-16 w-16 text-emerald-500" />
        <h2 className="text-2xl font-bold text-slate-800">Inspection terminée</h2>
        <p className="text-slate-500">{answered} / {total} questions traitées</p>
        <div className="flex gap-3 mt-2">
          <Button variant="outline" onClick={() => { setDone(false); setIndex(0); }}>
            Revoir depuis le début
          </Button>
          <Button onClick={() => router.push(`/auditor/audits/${id}`)}>
            Voir le rapport complet
          </Button>
        </div>
      </div>
    );
  }

  const pct = total > 0 ? Math.round((answered / total) * 100) : 0;

  return (
    <div className="flex flex-col h-screen bg-slate-50 max-w-lg mx-auto">
      {/* ── Header ── */}
      <div className="bg-white border-b border-slate-200 px-4 py-3 flex items-center gap-3">
        <button onClick={() => router.push(`/auditor/audits/${id}`)} className="text-slate-400 hover:text-slate-600">
          <ChevronLeft className="h-5 w-5" />
        </button>
        <div className="flex-1 min-w-0">
          <p className="text-xs text-slate-500 truncate">{audit.title}</p>
          <div className="mt-1 h-1.5 bg-slate-100 rounded-full overflow-hidden">
            <div className="h-full bg-blue-500 rounded-full transition-all" style={{ width: `${pct}%` }} />
          </div>
        </div>
        <span className="text-xs font-semibold text-slate-600 shrink-0">{index + 1} / {total}</span>
      </div>

      {/* ── Section label ── */}
      <div className="px-4 pt-3 pb-1">
        <span className="text-xs font-medium text-blue-600 bg-blue-50 px-2 py-0.5 rounded-full">
          {current.sectionTitle}
        </span>
      </div>

      {/* ── Question ── */}
      <div className="flex-1 overflow-y-auto px-4 py-3 space-y-4">
        <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
          <div className="flex items-start gap-2 mb-2">
            <span className={cn(
              "text-[10px] font-bold px-1.5 py-0.5 rounded shrink-0 mt-0.5",
              current.question.criticality === "critical" ? "bg-red-100 text-red-700" :
              current.question.criticality === "major"    ? "bg-orange-100 text-orange-700" :
              "bg-slate-100 text-slate-600"
            )}>
              {current.question.code}
            </span>
            {current.question.isMandatory && (
              <span className="text-[10px] bg-slate-100 text-slate-500 px-1.5 py-0.5 rounded shrink-0 mt-0.5">
                Obligatoire
              </span>
            )}
          </div>
          <p className="text-sm font-medium text-slate-800 leading-relaxed">{current.question.text}</p>
          {current.question.guidance && (
            <p className="text-xs text-slate-400 mt-2 italic">{current.question.guidance}</p>
          )}
        </div>

        {/* Current conformity if already set */}
        {response?.conformity && response.conformity !== "pending" && (
          <div className="text-xs text-center text-emerald-600 font-medium">
            ✓ Déjà évalué : {response.conformity}
          </div>
        )}

        {/* Notes */}
        <Textarea
          value={notes}
          onChange={e => setNotes(e.target.value)}
          placeholder="Notes, observations… (optionnel)"
          rows={3}
          className="text-sm bg-white"
        />

        {/* Utility buttons */}
        <div className="flex gap-2">
          <label className={cn(
            "flex items-center gap-1.5 text-xs font-medium px-3 py-2 rounded-lg border border-slate-200 bg-white cursor-pointer hover:bg-slate-50 transition-colors",
            uploadingPhoto && "opacity-50 pointer-events-none"
          )}>
            <Camera className="h-3.5 w-3.5 text-slate-500" />
            <span>{uploadingPhoto ? "Upload…" : "Photo"}</span>
            <input
              type="file"
              accept="image/*"
              capture="environment"
              className="sr-only"
              disabled={uploadingPhoto}
              onChange={e => { const f = e.target.files?.[0]; if (f) handlePhotoUpload(f); e.target.value = ""; }}
            />
          </label>

          <label className={cn(
            "flex items-center gap-1.5 text-xs font-medium px-3 py-2 rounded-lg border border-slate-200 bg-white cursor-pointer hover:bg-slate-50 transition-colors",
            uploadingPhoto && "opacity-50 pointer-events-none"
          )}>
            <Upload className="h-3.5 w-3.5 text-slate-500" />
            <span>Fichier</span>
            <input
              type="file"
              className="sr-only"
              disabled={uploadingPhoto}
              onChange={e => { const f = e.target.files?.[0]; if (f) handlePhotoUpload(f); e.target.value = ""; }}
            />
          </label>

          <button
            onClick={handleGps}
            disabled={gpsLoading}
            className="flex items-center gap-1.5 text-xs font-medium px-3 py-2 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 transition-colors disabled:opacity-50"
          >
            {gpsLoading ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Navigation className="h-3.5 w-3.5 text-slate-500" />}
            GPS
          </button>
        </div>
      </div>

      {/* ── Conformity buttons ── */}
      <div className="bg-white border-t border-slate-100 p-4 space-y-2">
        <div className="grid grid-cols-5 gap-1.5">
          {CONFORMITY.map(({ value, label, color, icon }) => (
            <button
              key={value}
              disabled={submitting}
              onClick={() => handleConformity(value)}
              className={cn(
                "flex flex-col items-center gap-1 py-3 rounded-xl border-2 transition-all text-[10px] font-semibold",
                response?.conformity === value
                  ? color + " border-current shadow-sm"
                  : "border-slate-200 bg-white text-slate-500 hover:border-slate-300 hover:bg-slate-50",
                submitting && "opacity-50 pointer-events-none"
              )}
            >
              {icon}
              <span className="leading-none">{label}</span>
            </button>
          ))}
        </div>

        {/* Navigation */}
        <div className="flex justify-between pt-1">
          <button
            onClick={() => { setIndex(i => Math.max(0, i - 1)); setNotes(""); }}
            disabled={index === 0}
            className="flex items-center gap-1 text-xs text-slate-400 hover:text-slate-600 disabled:opacity-30"
          >
            <ChevronLeft className="h-4 w-4" /> Précédent
          </button>
          <button
            onClick={() => { setIndex(i => Math.min(total - 1, i + 1)); setNotes(""); }}
            disabled={index === total - 1}
            className="flex items-center gap-1 text-xs text-slate-400 hover:text-slate-600 disabled:opacity-30"
          >
            Suivant <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>
    </div>
  );
}
