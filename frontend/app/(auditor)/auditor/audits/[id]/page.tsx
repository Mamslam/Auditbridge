"use client";

import React, { useEffect, useState, useCallback } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { auditsApi, CreateFindingRequest, CreateCapaRequest } from "@/lib/api/audits";
import type {
  AuditDetail, AuditSection, AuditQuestion, AuditResponse,
  AuditFinding, AuditCapa, AuditScore, AuditEvidence, ConformityRating,
  FindingType, CapaStatus,
} from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from "@/components/ui/dialog";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import {
  Accordion, AccordionContent, AccordionItem, AccordionTrigger,
} from "@/components/ui/accordion";
import { Progress } from "@/components/ui/progress";
import {
  CheckCircle2, AlertTriangle, AlertCircle, XCircle, Minus,
  Plus, FileDown, Copy, ExternalLink, RefreshCw, ChevronRight,
  Flag, FlagOff, Loader2, Upload, Trash2, File, Bot, Sparkles,
  Camera, MapPin, Navigation, PenLine,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

// ── Config maps ──────────────────────────────────────────────────────────────

const CONFORMITY_CFG: Record<ConformityRating, { label: string; color: string; icon: React.ReactNode }> = {
  compliant: { label: "Conforme",   color: "bg-emerald-500 hover:bg-emerald-600 text-white", icon: <CheckCircle2 className="h-3 w-3" /> },
  minor:     { label: "Mineur",     color: "bg-amber-400 hover:bg-amber-500 text-white",     icon: <AlertTriangle className="h-3 w-3" /> },
  major:     { label: "Majeur",     color: "bg-orange-500 hover:bg-orange-600 text-white",   icon: <AlertCircle className="h-3 w-3" /> },
  critical:  { label: "Critique",   color: "bg-red-600 hover:bg-red-700 text-white",         icon: <XCircle className="h-3 w-3" /> },
  na:        { label: "N/A",        color: "bg-slate-400 hover:bg-slate-500 text-white",     icon: <Minus className="h-3 w-3" /> },
  pending:   { label: "En attente", color: "bg-slate-200 hover:bg-slate-300 text-slate-700", icon: null },
};

const FINDING_TYPE_CFG: Record<FindingType, { label: string; color: string }> = {
  nc_critical: { label: "NC Critique", color: "bg-red-100 text-red-800 border-red-200" },
  nc_major:    { label: "NC Majeure",  color: "bg-orange-100 text-orange-800 border-orange-200" },
  nc_minor:    { label: "NC Mineure",  color: "bg-amber-100 text-amber-800 border-amber-200" },
  observation: { label: "Observation", color: "bg-blue-100 text-blue-800 border-blue-200" },
  ofi:         { label: "Opportunité", color: "bg-purple-100 text-purple-800 border-purple-200" },
};

const FINDING_STATUS_CFG: Record<string, { label: string; color: string }> = {
  open:         { label: "Ouvert",  color: "bg-red-50 text-red-700 border-red-200" },
  acknowledged: { label: "Accusé", color: "bg-amber-50 text-amber-700 border-amber-200" },
  closed:       { label: "Clôturé", color: "bg-emerald-50 text-emerald-700 border-emerald-200" },
};

const CAPA_STATUS_CFG: Record<CapaStatus, { label: string; color: string }> = {
  open:                 { label: "Ouvert",      color: "bg-slate-100 text-slate-700" },
  in_progress:          { label: "En cours",    color: "bg-blue-100 text-blue-700" },
  pending_verification: { label: "À vérifier",  color: "bg-amber-100 text-amber-700" },
  verified:             { label: "Vérifié",      color: "bg-emerald-100 text-emerald-700" },
  cancelled:            { label: "Annulé",       color: "bg-slate-100 text-slate-500 line-through" },
};

const STATUS_CFG: Record<string, { label: string; color: string }> = {
  draft:     { label: "Brouillon", color: "bg-slate-100 text-slate-600" },
  active:    { label: "Actif",     color: "bg-blue-100 text-blue-700" },
  submitted: { label: "Soumis",   color: "bg-amber-100 text-amber-700" },
  completed: { label: "Complété", color: "bg-emerald-100 text-emerald-700" },
  archived:  { label: "Archivé",  color: "bg-slate-100 text-slate-500" },
};

// ── Sub-components ────────────────────────────────────────────────────────────

function KpiCard({ label, value, sub }: { label: string; value: string | number; sub?: string }) {
  return (
    <div className="rounded-lg border bg-card p-4 flex flex-col gap-1">
      <p className="text-xs text-muted-foreground font-medium uppercase tracking-wide">{label}</p>
      <p className="text-2xl font-bold">{value}</p>
      {sub && <p className="text-xs text-muted-foreground">{sub}</p>}
    </div>
  );
}

function CapaStatusBadge({ status }: { status: CapaStatus }) {
  const cfg = CAPA_STATUS_CFG[status] ?? { label: status, color: "bg-slate-100" };
  return (
    <span className={cn("inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium", cfg.color)}>
      {cfg.label}
    </span>
  );
}

// ── Finding modal ─────────────────────────────────────────────────────────────

interface FindingModalProps {
  open: boolean;
  auditId: string;
  prefill?: { questionId?: string; responseId?: string };
  finding?: AuditFinding | null;
  onClose: () => void;
  onSaved: () => void;
}

function FindingModal({ open, auditId, prefill, finding, onClose, onSaved }: FindingModalProps) {
  const isEdit = !!finding;
  const [form, setForm] = useState({
    findingType: (finding?.findingType ?? "nc_minor") as FindingType,
    title: finding?.title ?? "",
    description: finding?.description ?? "",
    observedEvidence: finding?.observedEvidence ?? "",
    regulatoryRef: finding?.regulatoryRef ?? "",
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rawNotes, setRawNotes] = useState("");
  const [summarizing, setSummarizing] = useState(false);
  const [showAiInput, setShowAiInput] = useState(false);
  const [gps, setGps] = useState<{ lat: number; lng: number; name?: string } | null>(null);
  const [gpsLoading, setGpsLoading] = useState(false);

  useEffect(() => {
    if (open) {
      setForm({
        findingType: (finding?.findingType ?? "nc_minor") as FindingType,
        title: finding?.title ?? "",
        description: finding?.description ?? "",
        observedEvidence: finding?.observedEvidence ?? "",
        regulatoryRef: finding?.regulatoryRef ?? "",
      });
      setError(null);
      setRawNotes("");
      setShowAiInput(false);
      setGps(null);
    }
  }, [open, finding]);

  async function handleGps() {
    if (!navigator.geolocation) { toast.error("GPS non disponible"); return; }
    setGpsLoading(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setGps({ lat: pos.coords.latitude, lng: pos.coords.longitude });
        setGpsLoading(false);
        toast.success("Position capturée");
      },
      () => { toast.error("Impossible d'obtenir la position"); setGpsLoading(false); }
    );
  }

  async function handleAiSummarize() {
    if (!rawNotes.trim()) return;
    setSummarizing(true);
    try {
      const result = await auditsApi.summarizeFinding(auditId, rawNotes);
      setForm(f => ({
        ...f,
        findingType: (result.findingType as FindingType) ?? f.findingType,
        title: result.title || f.title,
        description: result.description || f.description,
        observedEvidence: result.observedEvidence || f.observedEvidence,
        regulatoryRef: result.regulatoryRef || f.regulatoryRef,
      }));
      setShowAiInput(false);
      setRawNotes("");
      toast.success("Constat structuré par l'IA");
    } catch {
      toast.error("Échec de la structuration IA");
    } finally {
      setSummarizing(false);
    }
  }

  async function handleSave() {
    if (!form.title.trim()) { setError("Le titre est requis."); return; }
    setSaving(true);
    setError(null);
    try {
      if (isEdit && finding) {
        await auditsApi.updateFinding(auditId, finding.id, {
          findingType: form.findingType,
          title: form.title,
          description: form.description || undefined,
          observedEvidence: form.observedEvidence || undefined,
          regulatoryRef: form.regulatoryRef || undefined,
        });
      } else {
        const req: CreateFindingRequest = {
          findingType: form.findingType,
          title: form.title,
          questionId: prefill?.questionId,
          responseId: prefill?.responseId,
          description: form.description || undefined,
          observedEvidence: form.observedEvidence || undefined,
          regulatoryRef: form.regulatoryRef || undefined,
          latitude: gps?.lat,
          longitude: gps?.lng,
          locationName: gps?.name || undefined,
        };
        await auditsApi.createFinding(auditId, req);
      }
      onSaved();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Erreur inconnue");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center justify-between">
            <span>{isEdit ? "Modifier le constat" : "Nouveau constat"}</span>
            {!isEdit && (
              <button
                type="button"
                onClick={() => setShowAiInput(v => !v)}
                className="flex items-center gap-1.5 text-xs text-purple-600 hover:text-purple-700 font-medium px-2 py-1 rounded-lg hover:bg-purple-50 transition-colors"
              >
                <Sparkles className="h-3.5 w-3.5" />
                Structurer avec l&apos;IA
              </button>
            )}
          </DialogTitle>
        </DialogHeader>

        {showAiInput && (
          <div className="bg-purple-50 border border-purple-200 rounded-xl p-4 space-y-2">
            <p className="text-xs text-purple-700 font-medium">Notes brutes de terrain</p>
            <Textarea
              rows={4}
              value={rawNotes}
              onChange={e => setRawNotes(e.target.value)}
              placeholder="Collez vos notes brutes ici — l'IA les transformera en constat structuré…"
              className="text-sm"
            />
            <div className="flex gap-2 justify-end">
              <Button size="sm" variant="outline" onClick={() => setShowAiInput(false)}>Annuler</Button>
              <Button size="sm" onClick={handleAiSummarize} disabled={summarizing || !rawNotes.trim()}
                className="bg-purple-600 hover:bg-purple-700 text-white">
                {summarizing ? <Loader2 className="mr-2 h-3.5 w-3.5 animate-spin" /> : <Sparkles className="mr-2 h-3.5 w-3.5" />}
                Structurer
              </Button>
            </div>
          </div>
        )}

        <div className="space-y-4 py-2">
          <div className="space-y-1">
            <Label>Type de constat</Label>
            <Select value={form.findingType} onValueChange={(v) => setForm(f => ({ ...f, findingType: v as FindingType }))}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {(Object.entries(FINDING_TYPE_CFG) as [FindingType, { label: string }][]).map(([k, { label }]) => (
                  <SelectItem key={k} value={k}>{label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-1">
            <Label>Titre <span className="text-red-500">*</span></Label>
            <Input value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} placeholder="Résumé du constat" />
          </div>
          <div className="space-y-1">
            <Label>Description</Label>
            <Textarea rows={3} value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} placeholder="Détails du problème constaté…" />
          </div>
          <div className="space-y-1">
            <Label>Preuve observée</Label>
            <Textarea rows={2} value={form.observedEvidence} onChange={e => setForm(f => ({ ...f, observedEvidence: e.target.value }))} placeholder="Document, enregistrement, observation directe…" />
          </div>
          <div className="space-y-1">
            <Label>Référence réglementaire</Label>
            <Input value={form.regulatoryRef} onChange={e => setForm(f => ({ ...f, regulatoryRef: e.target.value }))} placeholder="ISO 9001 §8.4.1, Art. 32 RGPD…" />
          </div>
          {!isEdit && (
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={handleGps}
                disabled={gpsLoading}
                className="flex items-center gap-1.5 text-xs text-slate-600 hover:text-slate-800 font-medium px-2.5 py-1.5 rounded-lg border border-slate-200 hover:bg-slate-50 transition-colors disabled:opacity-50"
              >
                {gpsLoading ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Navigation className="h-3.5 w-3.5" />}
                Localiser
              </button>
              {gps && (
                <span className="flex items-center gap-1 text-xs text-emerald-600 font-medium">
                  <MapPin className="h-3.5 w-3.5" />
                  {gps.lat.toFixed(5)}, {gps.lng.toFixed(5)}
                </span>
              )}
            </div>
          )}
          {error && <p className="text-sm text-red-600">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Annuler</Button>
          <Button onClick={handleSave} disabled={saving}>
            {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isEdit ? "Enregistrer" : "Créer le constat"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── CAPA modal ────────────────────────────────────────────────────────────────

interface CapaModalProps {
  open: boolean;
  auditId: string;
  findingId?: string;
  capa?: AuditCapa | null;
  onClose: () => void;
  onSaved: () => void;
}

function CapaModal({ open, auditId, findingId, capa, onClose, onSaved }: CapaModalProps) {
  const isEdit = !!capa;
  const [form, setForm] = useState({
    title: capa?.title ?? "",
    description: capa?.description ?? "",
    rootCause: capa?.rootCause ?? "",
    actionType: capa?.actionType ?? "corrective",
    priority: capa?.priority ?? "high",
    assignedToEmail: capa?.assignedToEmail ?? "",
    dueDate: capa?.dueDate ?? "",
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [suggesting, setSuggesting] = useState(false);

  useEffect(() => {
    if (open) {
      setForm({
        title: capa?.title ?? "",
        description: capa?.description ?? "",
        rootCause: capa?.rootCause ?? "",
        actionType: capa?.actionType ?? "corrective",
        priority: capa?.priority ?? "high",
        assignedToEmail: capa?.assignedToEmail ?? "",
        dueDate: capa?.dueDate ?? "",
      });
      setError(null);
    }
  }, [open, capa]);

  async function handleAiSuggest() {
    if (!findingId) return;
    setSuggesting(true);
    try {
      const result = await auditsApi.suggestCapa(auditId, findingId);
      setForm(f => ({
        ...f,
        title: result.title || f.title,
        description: result.description || f.description,
        rootCause: result.rootCause || f.rootCause,
        actionType: result.actionType || f.actionType,
        priority: result.priority || f.priority,
      }));
      toast.success("CAPA suggérée par l'IA");
    } catch {
      toast.error("Échec de la suggestion IA");
    } finally {
      setSuggesting(false);
    }
  }

  async function handleSave() {
    if (!form.title.trim()) { setError("Le titre est requis."); return; }
    setSaving(true);
    setError(null);
    try {
      if (isEdit && capa) {
        await auditsApi.updateCapa(auditId, capa.id, {
          title: form.title,
          actionType: form.actionType,
          priority: form.priority,
          description: form.description || undefined,
          rootCause: form.rootCause || undefined,
          assignedToEmail: form.assignedToEmail || undefined,
          dueDate: form.dueDate || undefined,
        });
      } else {
        const req: CreateCapaRequest = {
          title: form.title,
          findingId: findingId,
          actionType: form.actionType,
          priority: form.priority,
          description: form.description || undefined,
          assignedToEmail: form.assignedToEmail || undefined,
          dueDate: form.dueDate || undefined,
        };
        await auditsApi.createCapa(auditId, req);
      }
      onSaved();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Erreur inconnue");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center justify-between">
            <span>{isEdit ? "Modifier la CAPA" : "Nouvelle CAPA"}</span>
            {!isEdit && findingId && (
              <button
                type="button"
                onClick={handleAiSuggest}
                disabled={suggesting}
                className="flex items-center gap-1.5 text-xs text-purple-600 hover:text-purple-700 font-medium px-2 py-1 rounded-lg hover:bg-purple-50 transition-colors disabled:opacity-50"
              >
                {suggesting ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Sparkles className="h-3.5 w-3.5" />}
                Suggérer avec l&apos;IA
              </button>
            )}
          </DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-1">
            <Label>Titre <span className="text-red-500">*</span></Label>
            <Input value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} placeholder="Action corrective / préventive" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Type d&apos;action</Label>
              <Select value={form.actionType} onValueChange={(v: string | null) => setForm(f => ({ ...f, actionType: v ?? f.actionType }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="corrective">Corrective</SelectItem>
                  <SelectItem value="preventive">Préventive</SelectItem>
                  <SelectItem value="improvement">Amélioration</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label>Priorité</Label>
              <Select value={form.priority} onValueChange={(v: string | null) => setForm(f => ({ ...f, priority: v ?? f.priority }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="critical">Critique</SelectItem>
                  <SelectItem value="high">Haute</SelectItem>
                  <SelectItem value="medium">Moyenne</SelectItem>
                  <SelectItem value="low">Basse</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="space-y-1">
            <Label>Description</Label>
            <Textarea rows={3} value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
          </div>
          {isEdit && (
            <div className="space-y-1">
              <Label>Cause racine</Label>
              <Textarea rows={2} value={form.rootCause} onChange={e => setForm(f => ({ ...f, rootCause: e.target.value }))} />
            </div>
          )}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Responsable (email)</Label>
              <Input type="email" value={form.assignedToEmail} onChange={e => setForm(f => ({ ...f, assignedToEmail: e.target.value }))} />
            </div>
            <div className="space-y-1">
              <Label>Échéance</Label>
              <Input type="date" value={form.dueDate} onChange={e => setForm(f => ({ ...f, dueDate: e.target.value }))} />
            </div>
          </div>
          {error && <p className="text-sm text-red-600">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Annuler</Button>
          <Button onClick={handleSave} disabled={saving}>
            {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isEdit ? "Enregistrer" : "Créer la CAPA"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Signature modal ───────────────────────────────────────────────────────────

function SignatureModal({
  open, auditId, role, onClose, onSigned,
}: {
  open: boolean;
  auditId: string;
  role: "auditor" | "auditee";
  onClose: () => void;
  onSigned: () => void;
}) {
  const canvasRef = React.useRef<HTMLCanvasElement>(null);
  const [drawing, setDrawing] = useState(false);
  const [isEmpty, setIsEmpty] = useState(true);
  const [saving, setSaving] = useState(false);

  function getPos(e: React.MouseEvent | React.TouchEvent, canvas: HTMLCanvasElement) {
    const rect = canvas.getBoundingClientRect();
    if ("touches" in e) {
      return { x: e.touches[0].clientX - rect.left, y: e.touches[0].clientY - rect.top };
    }
    return { x: e.clientX - rect.left, y: e.clientY - rect.top };
  }

  function startDraw(e: React.MouseEvent | React.TouchEvent) {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d")!;
    const pos = getPos(e, canvas);
    ctx.beginPath();
    ctx.moveTo(pos.x, pos.y);
    setDrawing(true);
    setIsEmpty(false);
    e.preventDefault();
  }

  function draw(e: React.MouseEvent | React.TouchEvent) {
    if (!drawing) return;
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d")!;
    const pos = getPos(e, canvas);
    ctx.lineTo(pos.x, pos.y);
    ctx.strokeStyle = "#1e293b";
    ctx.lineWidth = 2;
    ctx.lineCap = "round";
    ctx.stroke();
    e.preventDefault();
  }

  function endDraw() { setDrawing(false); }

  function clearCanvas() {
    const canvas = canvasRef.current;
    if (!canvas) return;
    canvas.getContext("2d")!.clearRect(0, 0, canvas.width, canvas.height);
    setIsEmpty(true);
  }

  async function handleSave() {
    const canvas = canvasRef.current;
    if (!canvas || isEmpty) return;
    setSaving(true);
    try {
      const dataUrl = canvas.toDataURL("image/png");
      await auditsApi.signReport(auditId, dataUrl, role);
      toast.success(role === "auditor" ? "Signature auditeur enregistrée" : "Signature audité enregistrée");
      onSigned();
    } catch {
      toast.error("Échec de la signature");
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>
            {role === "auditor" ? "Signature auditeur" : "Signature audité"}
          </DialogTitle>
        </DialogHeader>
        <div className="space-y-3">
          <p className="text-xs text-slate-500">Signez dans le cadre ci-dessous</p>
          <canvas
            ref={canvasRef}
            width={380}
            height={160}
            className="w-full border-2 border-slate-200 rounded-lg bg-slate-50 touch-none cursor-crosshair"
            onMouseDown={startDraw}
            onMouseMove={draw}
            onMouseUp={endDraw}
            onMouseLeave={endDraw}
            onTouchStart={startDraw}
            onTouchMove={draw}
            onTouchEnd={endDraw}
          />
          <button
            type="button"
            onClick={clearCanvas}
            className="text-xs text-slate-400 hover:text-slate-600"
          >
            Effacer
          </button>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Annuler</Button>
          <Button onClick={handleSave} disabled={saving || isEmpty}>
            {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Valider la signature
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function AuditDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();

  const [audit, setAudit] = useState<AuditDetail | null>(null);
  const [score, setScore] = useState<AuditScore | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [conformityLoading, setConformityLoading] = useState<string | null>(null);
  const [flagLoading, setFlagLoading] = useState<string | null>(null);
  const [aiLoading, setAiLoading] = useState<string | null>(null);
  const [evidence, setEvidence] = useState<AuditEvidence[]>([]);
  const [uploadingFile, setUploadingFile] = useState(false);

  const [findingModal, setFindingModal] = useState<{
    open: boolean;
    prefill?: { questionId?: string; responseId?: string };
    editing?: AuditFinding | null;
  }>({ open: false });

  const [capaModal, setCapaModal] = useState<{
    open: boolean;
    findingId?: string;
    editing?: AuditCapa | null;
  }>({ open: false });

  const [askAiOpen, setAskAiOpen] = useState(false);
  const [askQuestion, setAskQuestion] = useState("");
  const [askAnswer, setAskAnswer] = useState<{
    answer: string; references?: string[]; confidence?: string; disclaimer?: string;
  } | null>(null);
  const [askLoading, setAskLoading] = useState(false);

  const [signModal, setSignModal] = useState<{ open: boolean; role: "auditor" | "auditee" } | null>(null);

  const load = useCallback(async () => {
    try {
      const [a, s, ev] = await Promise.all([
        auditsApi.getById(id),
        auditsApi.getScore(id).catch(() => null),
        auditsApi.getEvidence(id).catch(() => []),
      ]);
      setAudit(a);
      setScore(s);
      setEvidence(ev);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Erreur de chargement");
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { load(); }, [load]);

  function getResponse(questionId: string): AuditResponse | undefined {
    return audit?.responses.find(r => r.questionId === questionId);
  }

  async function handleConformity(questionId: string, conformity: ConformityRating) {
    if (!audit) return;
    const existing = getResponse(questionId);
    setConformityLoading(questionId);
    try {
      if (!existing) {
        await auditsApi.upsertResponse(audit.id, { questionId });
        const refreshed = await auditsApi.getById(audit.id);
        const newResp = refreshed.responses.find(r => r.questionId === questionId);
        if (newResp) {
          await auditsApi.setConformity(audit.id, newResp.id, { conformity });
        }
      } else {
        await auditsApi.setConformity(audit.id, existing.id, { conformity });
      }
      await load();
    } finally {
      setConformityLoading(null);
    }
  }

  async function handleAiAnalyze(questionId: string) {
    if (!audit) return;
    const resp = getResponse(questionId);
    if (!resp) return;
    setAiLoading(questionId);
    try {
      await auditsApi.analyzeResponse(audit.id, resp.id);
      await load();
      toast.success("Analyse IA terminée");
    } catch {
      toast.error("Analyse IA indisponible (clé API non configurée)");
    } finally {
      setAiLoading(null);
    }
  }

  async function handleFlag(questionId: string) {
    if (!audit) return;
    const existing = getResponse(questionId);
    if (!existing) return;
    setFlagLoading(questionId);
    try {
      await auditsApi.flagResponse(audit.id, existing.id, !existing.isFlagged);
      await load();
    } finally {
      setFlagLoading(null);
    }
  }

  async function handleFileUpload(file: File) {
    if (!audit) return;
    setUploadingFile(true);
    try {
      const { signedUrl, storagePath } = await auditsApi.presignUpload(audit.id, file.name);
      await fetch(signedUrl, { method: "PUT", body: file, headers: { "Content-Type": file.type } });
      await auditsApi.registerEvidence(audit.id, {
        fileName: file.name,
        storagePath,
        fileSizeBytes: file.size,
        mimeType: file.type || "application/octet-stream",
      });
      await load();
    } finally {
      setUploadingFile(false);
    }
  }

  async function handleDownloadEvidence(ev: AuditEvidence) {
    if (!audit) return;
    const { url } = await auditsApi.getDownloadUrl(audit.id, ev.id);
    window.open(url, "_blank");
  }

  async function handleDeleteEvidence(evidenceId: string) {
    if (!audit || !confirm("Supprimer ce fichier ?")) return;
    await auditsApi.deleteEvidence(audit.id, evidenceId);
    await load();
  }

  async function handleActivate() {
    if (!audit) return;
    setActionLoading("activate");
    try { await auditsApi.activate(audit.id); await load(); }
    finally { setActionLoading(null); }
  }

  async function handleComplete() {
    if (!audit) return;
    setActionLoading("complete");
    try { await auditsApi.complete(audit.id); await load(); }
    finally { setActionLoading(null); }
  }

  async function handleForceClose() {
    if (!audit) return;
    if (!confirm("Forcer la clôture ? Cette action ignore les validations de statut.")) return;
    setActionLoading("force");
    try { await auditsApi.forceClose(audit.id); await load(); }
    finally { setActionLoading(null); }
  }

  async function handleRefreshToken() {
    if (!audit) return;
    setActionLoading("token");
    try { await auditsApi.refreshToken(audit.id); await load(); }
    finally { setActionLoading(null); }
  }

  async function handleGeneratePdf() {
    if (!audit) return;
    setActionLoading("pdf");
    try {
      const resp = await auditsApi.generateReport(audit.id);
      if (!resp.ok) { alert("Erreur génération PDF"); return; }
      const blob = await resp.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `audit-${audit.title.replace(/\s+/g, "-")}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } finally {
      setActionLoading(null);
    }
  }

  async function handleAskAi() {
    if (!audit || !askQuestion.trim()) return;
    setAskLoading(true);
    setAskAnswer(null);
    try {
      const result = await auditsApi.askAudit(audit.id, askQuestion);
      setAskAnswer(result);
    } catch {
      toast.error("Impossible de contacter l'IA");
    } finally {
      setAskLoading(false);
    }
  }

  const totalQuestions = audit?.sections.reduce((s, sec) => s + sec.questions.length, 0) ?? 0;
  const answeredCount = audit?.responses.filter(r => r.conformity && r.conformity !== "pending").length ?? 0;
  const progressPct = totalQuestions > 0 ? Math.round((answeredCount / totalQuestions) * 100) : 0;
  const openFindings = audit?.findings.filter(f => f.status !== "closed").length ?? 0;
  const openCapas = audit?.capas.filter(c => c.status !== "verified" && c.status !== "cancelled").length ?? 0;
  const portalUrl = typeof window !== "undefined" && audit?.clientToken
    ? `${window.location.origin}/portal/${audit.clientToken}` : "";

  if (loading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error || !audit) {
    return (
      <div className="flex h-64 flex-col items-center justify-center gap-4">
        <p className="text-red-600">{error ?? "Audit introuvable"}</p>
        <Button variant="outline" onClick={() => router.back()}>Retour</Button>
      </div>
    );
  }

  const statusCfg = STATUS_CFG[audit.status] ?? { label: audit.status, color: "bg-slate-100" };

  return (
    <div className="mx-auto max-w-6xl space-y-6 px-4 py-6">
      {/* ── Header ── */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <button onClick={() => router.back()} className="text-sm text-muted-foreground hover:text-foreground">
              Audits
            </button>
            <ChevronRight className="h-4 w-4 text-muted-foreground" />
            <span className="text-sm font-medium">{audit.title}</span>
          </div>
          <h1 className="text-2xl font-bold">{audit.title}</h1>
          <div className="flex flex-wrap items-center gap-2">
            <Badge className={cn("text-xs", statusCfg.color)}>{statusCfg.label}</Badge>
            <span className="text-sm text-muted-foreground">{audit.referentialCode} · {audit.referentialName}</span>
            {audit.clientOrgName && <span className="text-sm text-muted-foreground">· {audit.clientOrgName}</span>}
            {audit.dueDate && (
              <span className="text-sm text-muted-foreground">
                · Échéance: {new Date(audit.dueDate).toLocaleDateString("fr-FR")}
              </span>
            )}
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Link
            href={`/auditor/audits/${id}/inspect`}
            className="inline-flex items-center gap-1.5 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors"
          >
            <Camera className="h-4 w-4" />
            Inspection terrain
          </Link>
          <Button
            variant="outline"
            size="sm"
            onClick={() => { setAskAiOpen(v => !v); setAskAnswer(null); setAskQuestion(""); }}
            className="border-purple-200 text-purple-700 hover:bg-purple-50"
          >
            <Bot className="mr-1.5 h-4 w-4" />
            Demander à l&apos;IA
          </Button>
          {audit.status === "draft" && (
            <Button onClick={handleActivate} disabled={actionLoading === "activate"}>
              {actionLoading === "activate" && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Activer l&apos;audit
            </Button>
          )}
          {audit.status === "active" && (
            <Button variant="outline" onClick={handleForceClose} disabled={actionLoading === "force"}>
              {actionLoading === "force" && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Forcer la clôture
            </Button>
          )}
          {audit.status === "submitted" && (
            <Button onClick={handleComplete} disabled={actionLoading === "complete"}>
              {actionLoading === "complete" && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Marquer complété
            </Button>
          )}
          {(audit.status === "completed" || audit.status === "submitted") && (
            <Button variant="outline" onClick={handleGeneratePdf} disabled={actionLoading === "pdf"}>
              {actionLoading === "pdf"
                ? <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                : <FileDown className="mr-2 h-4 w-4" />}
              Rapport PDF
            </Button>
          )}
          {audit.status === "completed" && (
            <Button variant="outline" size="sm"
              onClick={() => setSignModal({ open: true, role: "auditor" })}
              className="border-slate-300 text-slate-700 hover:bg-slate-50"
            >
              <PenLine className="mr-1.5 h-4 w-4" />
              Signer
            </Button>
          )}
        </div>
      </div>

      {/* ── Ask AI panel ── */}
      {askAiOpen && (
        <div className="rounded-xl border border-purple-200 bg-purple-50 p-4 space-y-3">
          <div className="flex items-center gap-2">
            <Bot className="h-4 w-4 text-purple-600" />
            <p className="text-sm font-semibold text-purple-800">
              Assistant IA — {audit.referentialName}
            </p>
            <button onClick={() => setAskAiOpen(false)} className="ml-auto text-purple-400 hover:text-purple-600 text-xs">✕</button>
          </div>
          <div className="flex gap-2">
            <Input
              value={askQuestion}
              onChange={e => setAskQuestion(e.target.value)}
              onKeyDown={e => e.key === "Enter" && !e.shiftKey && handleAskAi()}
              placeholder={`Ex: Que requiert ${audit.referentialCode} concernant la gestion des accès ?`}
              className="text-sm bg-white"
            />
            <Button onClick={handleAskAi} disabled={askLoading || !askQuestion.trim()}
              className="bg-purple-600 hover:bg-purple-700 text-white shrink-0">
              {askLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
            </Button>
          </div>
          {askAnswer && (
            <div className="bg-white border border-purple-100 rounded-lg p-4 space-y-2">
              <p className="text-sm text-slate-800 whitespace-pre-wrap">{askAnswer.answer}</p>
              {askAnswer.references && askAnswer.references.length > 0 && (
                <div className="flex flex-wrap gap-1 pt-1">
                  {askAnswer.references.map((ref, i) => (
                    <span key={i} className="text-[11px] bg-purple-100 text-purple-700 px-2 py-0.5 rounded-full font-medium">{ref}</span>
                  ))}
                </div>
              )}
              {askAnswer.disclaimer && (
                <p className="text-xs text-amber-600 border-t border-amber-100 pt-2 mt-2">{askAnswer.disclaimer}</p>
              )}
            </div>
          )}
        </div>
      )}

      {/* ── Client portal banner ── */}
      {audit.status === "active" && audit.clientToken && (
        <div className="rounded-lg border border-blue-200 bg-blue-50 p-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <p className="text-sm font-medium text-blue-900">Portail client actif</p>
              <p className="text-xs text-blue-700 font-mono mt-0.5 truncate max-w-sm">{portalUrl}</p>
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" onClick={() => navigator.clipboard.writeText(portalUrl)}>
                <Copy className="h-3.5 w-3.5 mr-1.5" /> Copier
              </Button>
              <Button size="sm" variant="outline" onClick={() => window.open(portalUrl, "_blank")}>
                <ExternalLink className="h-3.5 w-3.5 mr-1.5" /> Ouvrir
              </Button>
              <Button size="sm" variant="ghost" onClick={handleRefreshToken} disabled={actionLoading === "token"}>
                <RefreshCw className={cn("h-3.5 w-3.5 mr-1.5", actionLoading === "token" && "animate-spin")} />
                Renouveler
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* ── KPI strip ── */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <KpiCard label="Progression" value={`${progressPct}%`} sub={`${answeredCount} / ${totalQuestions} questions`} />
        <KpiCard
          label="Score conformité"
          value={score ? `${score.globalScore.toFixed(1)}%` : "—"}
          sub={score ? `${score.conformCount} conformes` : "calcul en attente"}
        />
        <KpiCard label="Constats ouverts" value={openFindings} sub={`${audit.findings.length} total`} />
        <KpiCard label="CAPAs actives" value={openCapas} sub={`${audit.capas.length} total`} />
      </div>
      <Progress value={progressPct} className="h-2" />

      {/* ── Tabs ── */}
      <Tabs defaultValue="questionnaire">
        <TabsList>
          <TabsTrigger value="questionnaire">Questionnaire</TabsTrigger>
          <TabsTrigger value="findings">
            Constats
            {openFindings > 0 && (
              <Badge className="ml-1.5 h-4 min-w-4 px-1 text-xs">{openFindings}</Badge>
            )}
          </TabsTrigger>
          <TabsTrigger value="capas">
            CAPAs
            {openCapas > 0 && (
              <Badge className="ml-1.5 h-4 min-w-4 px-1 text-xs">{openCapas}</Badge>
            )}
          </TabsTrigger>
          <TabsTrigger value="score">Score</TabsTrigger>
          <TabsTrigger value="evidence">
            Pièces jointes
            {evidence.length > 0 && (
              <Badge className="ml-1.5 h-4 min-w-4 px-1 text-xs">{evidence.length}</Badge>
            )}
          </TabsTrigger>
        </TabsList>

        {/* ── Questionnaire ── */}
        <TabsContent value="questionnaire" className="mt-4">
          <Accordion defaultValue={audit.sections.map(s => s.id)} className="space-y-2">
            {audit.sections.map((section: AuditSection) => {
              const sectionAnswered = section.questions.filter(q => {
                const r = getResponse(q.id);
                return r?.conformity && r.conformity !== "pending";
              }).length;

              return (
                <AccordionItem key={section.id} value={section.id} className="rounded-lg border bg-card px-4">
                  <AccordionTrigger className="py-4 hover:no-underline">
                    <div className="flex items-center justify-between w-full pr-4">
                      <span className="font-semibold text-left">{section.title}</span>
                      <span className="text-sm text-muted-foreground ml-4 shrink-0">
                        {sectionAnswered} / {section.questions.length}
                      </span>
                    </div>
                  </AccordionTrigger>
                  <AccordionContent className="pb-4">
                    <div className="space-y-4">
                      {section.questions.map((question: AuditQuestion) => {
                        const resp = getResponse(question.id);
                        const isBusy = conformityLoading === question.id;
                        const isFlagBusy = flagLoading === question.id;

                        return (
                          <div
                            key={question.id}
                            className={cn(
                              "rounded-md border p-4 space-y-3 transition-colors",
                              resp?.isFlagged && "border-amber-300 bg-amber-50/50",
                            )}
                          >
                            <div className="flex items-start justify-between gap-2">
                              <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-2 flex-wrap">
                                  <span className="text-xs font-mono text-muted-foreground">{question.code}</span>
                                  <Badge
                                    variant="outline"
                                    className={cn(
                                      "text-xs",
                                      question.criticality === "critical" && "border-red-300 text-red-700",
                                      question.criticality === "major" && "border-orange-300 text-orange-700",
                                      question.criticality === "minor" && "border-amber-300 text-amber-700",
                                      question.criticality === "info" && "border-blue-300 text-blue-700",
                                    )}
                                  >
                                    {question.criticality}
                                  </Badge>
                                  {question.isMandatory && (
                                    <span className="text-xs text-red-500 font-medium">Obligatoire</span>
                                  )}
                                </div>
                                <p className="mt-1 text-sm">{question.text}</p>
                                {question.guidance && (
                                  <p className="mt-1 text-xs text-muted-foreground italic">{question.guidance}</p>
                                )}
                              </div>
                              <button
                                className="shrink-0 mt-0.5 disabled:opacity-40"
                                onClick={() => handleFlag(question.id)}
                                disabled={!resp || isFlagBusy}
                                title={resp?.isFlagged ? "Retirer le drapeau" : "Signaler"}
                              >
                                {isFlagBusy
                                  ? <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
                                  : resp?.isFlagged
                                    ? <Flag className="h-4 w-4 text-amber-500" />
                                    : <FlagOff className="h-4 w-4 text-muted-foreground hover:text-amber-500" />}
                              </button>
                            </div>

                            {/* Conformity buttons */}
                            <div className="flex flex-wrap gap-1.5">
                              {(Object.entries(CONFORMITY_CFG) as [ConformityRating, typeof CONFORMITY_CFG[ConformityRating]][]).map(
                                ([key, cfg]) => (
                                  <button
                                    key={key}
                                    onClick={() => handleConformity(question.id, key)}
                                    disabled={isBusy}
                                    className={cn(
                                      "inline-flex items-center gap-1 rounded-md px-2.5 py-1 text-xs font-medium transition-all",
                                      cfg.color,
                                      resp?.conformity === key
                                        ? "ring-2 ring-offset-1 ring-current opacity-100"
                                        : "opacity-60 hover:opacity-90",
                                      isBusy && "cursor-not-allowed",
                                    )}
                                  >
                                    {isBusy
                                      ? <Loader2 className="h-3 w-3 animate-spin" />
                                      : cfg.icon}
                                    {cfg.label}
                                  </button>
                                ),
                              )}
                            </div>

                            {resp?.auditorComment && (
                              <p className="text-xs text-muted-foreground border-l-2 border-slate-200 pl-2">
                                {resp.auditorComment}
                              </p>
                            )}

                            {resp?.aiAnalysis && (() => {
                              try {
                                const ai = JSON.parse(resp.aiAnalysis);
                                return (
                                  <div className="rounded-lg bg-purple-50 border border-purple-100 p-3 space-y-1">
                                    <div className="flex items-center gap-1.5 text-xs font-semibold text-purple-700">
                                      <Bot className="h-3.5 w-3.5" /> Analyse IA
                                    </div>
                                    <p className="text-xs text-purple-800">{ai.auditor_recommendation}</p>
                                    {ai.gaps?.length > 0 && (
                                      <p className="text-xs text-purple-600">Écarts : {ai.gaps.join(", ")}</p>
                                    )}
                                    <div className="flex gap-2 text-[10px] text-purple-500">
                                      <span>Conformité : <strong>{ai.conformity}</strong></span>
                                      <span>Risque : <strong>{ai.regulatory_risk}</strong></span>
                                      <span>Confiance : <strong>{Math.round((ai.confidence ?? 0) * 100)}%</strong></span>
                                    </div>
                                  </div>
                                );
                              } catch { return null; }
                            })()}

                            <div className="flex justify-end gap-1">
                              {resp && (
                                <Button
                                  size="sm" variant="ghost"
                                  className="h-7 text-xs gap-1 text-purple-600 hover:text-purple-700 hover:bg-purple-50"
                                  disabled={aiLoading === question.id}
                                  onClick={() => handleAiAnalyze(question.id)}
                                >
                                  {aiLoading === question.id
                                    ? <Loader2 className="h-3 w-3 animate-spin" />
                                    : <Sparkles className="h-3 w-3" />}
                                  IA
                                </Button>
                              )}
                              <Button
                                size="sm" variant="ghost" className="h-7 text-xs gap-1"
                                onClick={() => setFindingModal({
                                  open: true,
                                  prefill: { questionId: question.id, responseId: resp?.id },
                                })}
                              >
                                <Plus className="h-3 w-3" /> Constat
                              </Button>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </AccordionContent>
                </AccordionItem>
              );
            })}
          </Accordion>
        </TabsContent>

        {/* ── Findings ── */}
        <TabsContent value="findings" className="mt-4 space-y-3">
          <div className="flex justify-between items-center">
            <h2 className="text-lg font-semibold">Constats ({audit.findings.length})</h2>
            <Button size="sm" onClick={() => setFindingModal({ open: true })}>
              <Plus className="mr-1.5 h-4 w-4" /> Nouveau constat
            </Button>
          </div>

          {audit.findings.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
              <AlertTriangle className="mx-auto h-8 w-8 mb-2 opacity-40" />
              <p>Aucun constat pour cet audit.</p>
            </div>
          ) : (
            <div className="space-y-3">
              {[...audit.findings]
                .sort((a, b) => {
                  const order = ["nc_critical", "nc_major", "nc_minor", "observation", "ofi"];
                  return order.indexOf(a.findingType) - order.indexOf(b.findingType);
                })
                .map((finding: AuditFinding) => {
                  const typeCfg = FINDING_TYPE_CFG[finding.findingType];
                  const sCfg = FINDING_STATUS_CFG[finding.status];
                  return (
                    <div key={finding.id} className="rounded-lg border bg-card p-4 space-y-3">
                      <div className="flex items-start justify-between gap-3">
                        <div className="flex-1 min-w-0 space-y-1">
                          <div className="flex flex-wrap items-center gap-2">
                            <Badge variant="outline" className={cn("text-xs", typeCfg.color)}>{typeCfg.label}</Badge>
                            <Badge variant="outline" className={cn("text-xs", sCfg.color)}>{sCfg.label}</Badge>
                          </div>
                          <p className="font-medium">{finding.title}</p>
                          {finding.description && (
                            <p className="text-sm text-muted-foreground">{finding.description}</p>
                          )}
                          {finding.observedEvidence && (
                            <p className="text-xs text-muted-foreground border-l-2 border-slate-200 pl-2">
                              <span className="font-medium">Preuve:</span> {finding.observedEvidence}
                            </p>
                          )}
                          {finding.regulatoryRef && (
                            <p className="text-xs text-muted-foreground">
                              <span className="font-medium">Réf.:</span> {finding.regulatoryRef}
                            </p>
                          )}
                        </div>
                        <div className="flex gap-1 shrink-0 flex-wrap">
                          <Button
                            size="sm" variant="ghost" className="h-7 text-xs"
                            onClick={() => setFindingModal({ open: true, editing: finding })}
                          >
                            Modifier
                          </Button>
                          {finding.status === "open" && (
                            <Button
                              size="sm" variant="outline" className="h-7 text-xs"
                              onClick={async () => { await auditsApi.acknowledgeFinding(audit.id, finding.id); await load(); }}
                            >
                              Accuser réception
                            </Button>
                          )}
                          {finding.status === "acknowledged" && (
                            <Button
                              size="sm" variant="outline" className="h-7 text-xs"
                              onClick={async () => { await auditsApi.closeFinding(audit.id, finding.id); await load(); }}
                            >
                              Clôturer
                            </Button>
                          )}
                        </div>
                      </div>

                      <div className="pl-2 border-l-2 border-slate-100 space-y-1.5">
                        {finding.capas.length === 0 ? (
                          <p className="text-xs text-muted-foreground">Aucune CAPA liée.</p>
                        ) : finding.capas.map(c => (
                          <div key={c.id} className="flex items-center justify-between text-xs">
                            <span className="truncate">{c.title}</span>
                            <CapaStatusBadge status={c.status} />
                          </div>
                        ))}
                        <Button
                          size="sm" variant="ghost" className="h-6 text-xs gap-1 mt-1"
                          onClick={() => setCapaModal({ open: true, findingId: finding.id })}
                        >
                          <Plus className="h-3 w-3" /> CAPA depuis constat
                        </Button>
                      </div>
                    </div>
                  );
                })}
            </div>
          )}
        </TabsContent>

        {/* ── CAPAs ── */}
        <TabsContent value="capas" className="mt-4 space-y-3">
          <div className="flex justify-between items-center">
            <h2 className="text-lg font-semibold">CAPAs ({audit.capas.length})</h2>
            <Button size="sm" onClick={() => setCapaModal({ open: true })}>
              <Plus className="mr-1.5 h-4 w-4" /> Nouvelle CAPA
            </Button>
          </div>

          {audit.capas.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
              <CheckCircle2 className="mx-auto h-8 w-8 mb-2 opacity-40" />
              <p>Aucune action corrective/préventive.</p>
            </div>
          ) : (
            <div className="space-y-3">
              {[...audit.capas]
                .sort((a, b) => {
                  const prio = ["critical", "high", "medium", "low"];
                  return prio.indexOf(a.priority) - prio.indexOf(b.priority);
                })
                .map((capa: AuditCapa) => (
                  <div key={capa.id} className="rounded-lg border bg-card p-4 space-y-2">
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex-1 min-w-0 space-y-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <CapaStatusBadge status={capa.status} />
                          <Badge variant="outline" className="text-xs capitalize">{capa.priority}</Badge>
                          <Badge variant="outline" className="text-xs capitalize">{capa.actionType}</Badge>
                          {capa.aiGenerated && (
                            <Badge variant="outline" className="text-xs bg-purple-50 text-purple-700 border-purple-200">IA</Badge>
                          )}
                        </div>
                        <p className="font-medium">{capa.title}</p>
                        {capa.description && (
                          <p className="text-sm text-muted-foreground">{capa.description}</p>
                        )}
                        {capa.rootCause && (
                          <p className="text-xs text-muted-foreground">
                            <span className="font-medium">Cause racine:</span> {capa.rootCause}
                          </p>
                        )}
                        <div className="flex flex-wrap gap-3 text-xs text-muted-foreground">
                          {capa.assignedToEmail && <span>→ {capa.assignedToEmail}</span>}
                          {capa.dueDate && <span>Échéance: {new Date(capa.dueDate).toLocaleDateString("fr-FR")}</span>}
                          {capa.completedAt && <span>Complétée: {new Date(capa.completedAt).toLocaleDateString("fr-FR")}</span>}
                        </div>
                      </div>
                      <div className="flex gap-1 shrink-0 flex-wrap justify-end">
                        <Button
                          size="sm" variant="ghost" className="h-7 text-xs"
                          onClick={() => setCapaModal({ open: true, editing: capa })}
                        >
                          Modifier
                        </Button>
                        {capa.status === "open" && (
                          <Button
                            size="sm" variant="outline" className="h-7 text-xs"
                            onClick={async () => { await auditsApi.updateCapaStatus(audit.id, capa.id, "in_progress"); await load(); }}
                          >
                            Démarrer
                          </Button>
                        )}
                        {capa.status === "in_progress" && (
                          <Button
                            size="sm" variant="outline" className="h-7 text-xs"
                            onClick={async () => { await auditsApi.updateCapaStatus(audit.id, capa.id, "pending_verification"); await load(); }}
                          >
                            Soumettre vérif.
                          </Button>
                        )}
                        {capa.status === "pending_verification" && (
                          <Button
                            size="sm" variant="outline" className="h-7 text-xs text-emerald-700 border-emerald-300"
                            onClick={async () => { await auditsApi.updateCapaStatus(audit.id, capa.id, "verified"); await load(); }}
                          >
                            Vérifier ✓
                          </Button>
                        )}
                        {capa.status !== "verified" && capa.status !== "cancelled" && (
                          <Button
                            size="sm" variant="ghost" className="h-7 text-xs text-red-600"
                            onClick={async () => { await auditsApi.updateCapaStatus(audit.id, capa.id, "cancelled"); await load(); }}
                          >
                            Annuler
                          </Button>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
            </div>
          )}
        </TabsContent>

        {/* ── Score ── */}
        <TabsContent value="score" className="mt-4">
          {!score ? (
            <div className="flex h-40 items-center justify-center text-muted-foreground">
              Score non disponible (commencez à qualifier les réponses)
            </div>
          ) : (
            <div className="space-y-6">
              <div className="rounded-xl border bg-card p-6 flex flex-col sm:flex-row items-center gap-6">
                <div className="flex flex-col items-center">
                  <div className={cn(
                    "flex h-28 w-28 items-center justify-center rounded-full text-3xl font-bold",
                    score.globalScore >= 80 && "bg-emerald-100 text-emerald-800",
                    score.globalScore >= 60 && score.globalScore < 80 && "bg-amber-100 text-amber-800",
                    score.globalScore < 60 && "bg-red-100 text-red-800",
                  )}>
                    {score.globalScore.toFixed(0)}%
                  </div>
                  <p className="mt-2 text-sm text-muted-foreground">Score global</p>
                </div>
                <div className="flex-1 grid grid-cols-2 sm:grid-cols-3 gap-3 w-full">
                  <div className="text-center">
                    <p className="text-2xl font-bold text-emerald-600">{score.conformCount}</p>
                    <p className="text-xs text-muted-foreground">Conformes</p>
                  </div>
                  <div className="text-center">
                    <p className="text-2xl font-bold text-amber-500">{score.minorCount}</p>
                    <p className="text-xs text-muted-foreground">NC Mineures</p>
                  </div>
                  <div className="text-center">
                    <p className="text-2xl font-bold text-orange-500">{score.majorCount}</p>
                    <p className="text-xs text-muted-foreground">NC Majeures</p>
                  </div>
                  <div className="text-center">
                    <p className="text-2xl font-bold text-red-600">{score.criticalCount}</p>
                    <p className="text-xs text-muted-foreground">NC Critiques</p>
                  </div>
                  <div className="text-center">
                    <p className="text-2xl font-bold text-slate-400">{score.naCount}</p>
                    <p className="text-xs text-muted-foreground">N/A</p>
                  </div>
                  <div className="text-center">
                    <p className="text-2xl font-bold text-slate-400">{score.pendingCount}</p>
                    <p className="text-xs text-muted-foreground">En attente</p>
                  </div>
                </div>
              </div>

              <div className="rounded-lg border bg-card overflow-hidden">
                <div className="px-4 py-3 border-b bg-muted/30">
                  <h3 className="font-semibold text-sm">Scores par section</h3>
                </div>
                <div className="divide-y">
                  {score.sectionScores.map(s => (
                    <div key={s.sectionId} className="px-4 py-3 space-y-1.5">
                      <div className="flex items-center justify-between text-sm">
                        <span className="font-medium truncate">{s.title}</span>
                        <span className={cn(
                          "font-bold ml-3 shrink-0",
                          s.conformityPct == null && "text-muted-foreground",
                          s.conformityPct != null && s.conformityPct >= 80 && "text-emerald-600",
                          s.conformityPct != null && s.conformityPct >= 60 && s.conformityPct < 80 && "text-amber-600",
                          s.conformityPct != null && s.conformityPct < 60 && "text-red-600",
                        )}>
                          {s.conformityPct != null ? `${s.conformityPct.toFixed(0)}%` : "—"}
                        </span>
                      </div>
                      <Progress value={s.conformityPct ?? 0} className="h-1.5" />
                      <div className="flex gap-3 text-xs text-muted-foreground">
                        <span>{s.conformCount} conf.</span>
                        {s.minorCount > 0 && <span className="text-amber-600">{s.minorCount} min.</span>}
                        {s.majorCount > 0 && <span className="text-orange-600">{s.majorCount} maj.</span>}
                        {s.criticalCount > 0 && <span className="text-red-600">{s.criticalCount} crit.</span>}
                        <span className="ml-auto">{s.totalQuestions} q.</span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </TabsContent>
        {/* ── Evidence ── */}
        <TabsContent value="evidence" className="mt-4 space-y-3">
          <div className="flex justify-between items-center">
            <h2 className="text-lg font-semibold">Pièces jointes ({evidence.length})</h2>
            <div className="flex gap-2">
              {/* Camera capture — mobile field inspection */}
              <label className={cn(
                "inline-flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium cursor-pointer",
                "border border-slate-200 bg-white hover:bg-slate-50 transition-colors text-slate-700",
                uploadingFile && "opacity-50 pointer-events-none",
              )}>
                <Camera className="h-4 w-4" />
                <span className="hidden sm:inline">Photo</span>
                <input
                  type="file"
                  accept="image/*"
                  capture="environment"
                  className="sr-only"
                  disabled={uploadingFile}
                  onChange={e => { const f = e.target.files?.[0]; if (f) handleFileUpload(f); e.target.value = ""; }}
                />
              </label>
              <label className={cn(
                "inline-flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium cursor-pointer",
                "bg-primary text-primary-foreground hover:bg-primary/90 transition-colors",
                uploadingFile && "opacity-50 pointer-events-none",
              )}>
                {uploadingFile
                  ? <Loader2 className="h-4 w-4 animate-spin" />
                  : <Upload className="h-4 w-4" />}
                {uploadingFile ? "Téléversement…" : "Ajouter un fichier"}
                <input
                  type="file"
                  className="sr-only"
                  disabled={uploadingFile}
                  onChange={e => { const f = e.target.files?.[0]; if (f) handleFileUpload(f); e.target.value = ""; }}
                />
              </label>
            </div>
          </div>

          {evidence.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
              <File className="mx-auto h-8 w-8 mb-2 opacity-40" />
              <p>Aucune pièce jointe. Glissez-déposez ou cliquez pour ajouter.</p>
            </div>
          ) : (
            <div className="rounded-lg border bg-card divide-y">
              {evidence.map((ev: AuditEvidence) => {
                const sizeKb = (ev.fileSizeBytes / 1024).toFixed(0);
                const sizeMb = (ev.fileSizeBytes / (1024 * 1024)).toFixed(1);
                const displaySize = ev.fileSizeBytes > 1024 * 1024 ? `${sizeMb} Mo` : `${sizeKb} Ko`;
                return (
                  <div key={ev.id} className="flex items-center gap-3 px-4 py-3">
                    <File className="h-5 w-5 text-muted-foreground shrink-0" />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium truncate">{ev.fileName}</p>
                      <p className="text-xs text-muted-foreground">
                        {displaySize} · {ev.mimeType} · {new Date(ev.createdAt).toLocaleDateString("fr-FR")}
                        {ev.description && ` · ${ev.description}`}
                      </p>
                    </div>
                    <div className="flex gap-1 shrink-0">
                      <Button
                        size="sm" variant="ghost" className="h-7 w-7 p-0"
                        onClick={() => handleDownloadEvidence(ev)}
                        title="Télécharger"
                      >
                        <FileDown className="h-4 w-4" />
                      </Button>
                      <Button
                        size="sm" variant="ghost" className="h-7 w-7 p-0 text-red-500 hover:text-red-700"
                        onClick={() => handleDeleteEvidence(ev.id)}
                        title="Supprimer"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </TabsContent>
      </Tabs>

      {/* ── Modals ── */}
      <FindingModal
        open={findingModal.open}
        auditId={audit.id}
        prefill={findingModal.prefill}
        finding={findingModal.editing}
        onClose={() => setFindingModal({ open: false })}
        onSaved={async () => { setFindingModal({ open: false }); await load(); }}
      />
      <CapaModal
        open={capaModal.open}
        auditId={audit.id}
        findingId={capaModal.findingId}
        capa={capaModal.editing}
        onClose={() => setCapaModal({ open: false })}
        onSaved={async () => { setCapaModal({ open: false }); await load(); }}
      />
      {signModal && (
        <SignatureModal
          open={signModal.open}
          auditId={audit.id}
          role={signModal.role}
          onClose={() => setSignModal(null)}
          onSigned={() => { setSignModal(null); toast.success("Rapport signé"); }}
        />
      )}
    </div>
  );
}
