"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Shield, CheckCircle2, ArrowRight, ArrowLeft, Loader2, BookOpen, Users, Calendar, Building2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { referentialsApi } from "@/lib/api/referentials";
import { auditsApi } from "@/lib/api/audits";
import { organizationsApi } from "@/lib/api/organizations";
import { toast } from "sonner";
import { cn } from "@/lib/utils";

// ── Types ──────────────────────────────────────────────────────────────────

type Step = 1 | 2 | 3 | 4;

interface OrgForm {
  name: string;
  industry: string;
  size: string;
}

interface AuditForm {
  title: string;
  dueDate: string;
  clientEmail: string;
}

const INDUSTRIES = [
  "Pharmaceutique & Médical",
  "Alimentaire & Agriculture",
  "Industrie & Manufacture",
  "Cybersécurité & IT",
  "Finance & Assurance",
  "Santé & Hôpital",
  "Aéronautique & Défense",
  "Environnement & Énergie",
  "Autre",
];

const SIZES = [
  { label: "1–10 personnes", value: "small" },
  { label: "11–50 personnes", value: "medium" },
  { label: "51–200 personnes", value: "large" },
  { label: "200+ personnes", value: "enterprise" },
];

// Top referentials to suggest
const SUGGESTED_REFS = [
  { id: "iso-9001", name: "ISO 9001:2015", category: "Qualité" },
  { id: "iso-27001", name: "ISO 27001:2022", category: "Cybersécurité" },
  { id: "iso-14001", name: "ISO 14001:2015", category: "Environnement" },
  { id: "iso-45001", name: "ISO 45001:2018", category: "Santé & Sécurité" },
  { id: "iso-22000", name: "ISO 22000:2018", category: "Alimentaire" },
  { id: "gmp-annex11", name: "GMP / EU-GMP Annex 11", category: "Pharmaceutique" },
];

// ── Step indicator ─────────────────────────────────────────────────────────

function StepDot({ n, current, done }: { n: number; current: Step; done: boolean }) {
  const active = n === current;
  return (
    <div className={cn(
      "h-8 w-8 rounded-full flex items-center justify-center text-sm font-bold border-2 transition-all",
      done ? "bg-emerald-500 border-emerald-500 text-white" :
      active ? "bg-blue-600 border-blue-600 text-white" :
      "bg-white border-slate-200 text-slate-400"
    )}>
      {done ? <CheckCircle2 className="h-4 w-4" /> : n}
    </div>
  );
}

// ── Main ──────────────────────────────────────────────────────────────────

export default function OnboardingPage() {
  const router = useRouter();
  const [step, setStep] = useState<Step>(1);
  const [loading, setLoading] = useState(false);

  const [org, setOrg] = useState<OrgForm>({ name: "", industry: "", size: "" });
  const [selectedRefIds, setSelectedRefIds] = useState<string[]>([]);
  const [auditForm, setAuditForm] = useState<AuditForm>({ title: "", dueDate: "", clientEmail: "" });
  const [inviteEmail, setInviteEmail] = useState("");
  const [invited, setInvited] = useState<string[]>([]);

  // ── Step 1: Org ──
  async function handleOrgNext() {
    if (!org.name.trim()) { toast.error("Veuillez saisir le nom de votre organisation"); return; }
    setLoading(true);
    try {
      await organizationsApi.update({ name: org.name.trim() });
      setStep(2);
    } catch {
      // Org may already exist — still allow to proceed
      setStep(2);
    } finally {
      setLoading(false);
    }
  }

  // ── Step 2: Referentials ──
  async function handleRefsNext() {
    if (selectedRefIds.length === 0) { toast.error("Sélectionnez au moins un référentiel"); return; }
    setLoading(true);
    try {
      // Fetch referentials and duplicate selected ones into the org
      const all = await referentialsApi.getAll();
      const toImport = all.filter(r =>
        selectedRefIds.some(sid =>
          r.name.toLowerCase().includes(sid.split("-").slice(0, 2).join(" ").toLowerCase()) ||
          r.slug === sid
        )
      );
      for (const ref of toImport.slice(0, 3)) {
        await referentialsApi.duplicate(ref.id);
      }
    } catch {
      // Silently continue — refs can be added later
    } finally {
      setLoading(false);
      setStep(3);
    }
  }

  // ── Step 3: First audit ──
  async function handleAuditNext() {
    if (!auditForm.title.trim()) { toast.error("Veuillez saisir un titre d'audit"); return; }
    setLoading(true);
    try {
      const all = await referentialsApi.getAll();
      const ref = all[0];
      if (ref) {
        await auditsApi.create({
          referentialId: ref.id,
          title: auditForm.title.trim(),
          clientEmail: auditForm.clientEmail || undefined,
          dueDate: auditForm.dueDate || undefined,
        });
      }
    } catch {
      // Continue even if creation fails
    } finally {
      setLoading(false);
      setStep(4);
    }
  }

  // ── Step 4: Invite ──
  async function handleInvite() {
    if (!inviteEmail.trim()) return;
    try {
      await organizationsApi.inviteMember({ email: inviteEmail.trim(), role: "auditor" });
      setInvited(prev => [...prev, inviteEmail.trim()]);
      setInviteEmail("");
      toast.success(`Invitation envoyée à ${inviteEmail.trim()}`);
    } catch {
      toast.error("Erreur lors de l'invitation");
    }
  }

  function handleFinish() {
    router.push("/auditor/dashboard");
  }

  const steps = [
    { n: 1 as Step, label: "Organisation", icon: Building2 },
    { n: 2 as Step, label: "Référentiels", icon: BookOpen },
    { n: 3 as Step, label: "Premier audit", icon: Calendar },
    { n: 4 as Step, label: "Équipe", icon: Users },
  ];

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-slate-50 flex flex-col items-center justify-center p-6">
      {/* Logo */}
      <div className="flex items-center gap-2 mb-10">
        <div className="h-9 w-9 rounded-xl bg-blue-600 flex items-center justify-center">
          <Shield className="h-5 w-5 text-white" />
        </div>
        <span className="text-xl font-bold text-slate-900">AuditBridge</span>
      </div>

      {/* Step bar */}
      <div className="flex items-center gap-0 mb-10">
        {steps.map((s, i) => (
          <div key={s.n} className="flex items-center">
            <div className="flex flex-col items-center gap-1">
              <StepDot n={s.n} current={step} done={step > s.n} />
              <span className={cn(
                "text-[10px] font-medium hidden sm:block",
                step === s.n ? "text-blue-600" : step > s.n ? "text-emerald-500" : "text-slate-400"
              )}>
                {s.label}
              </span>
            </div>
            {i < steps.length - 1 && (
              <div className={cn(
                "h-0.5 w-12 sm:w-20 mx-1 mb-4 transition-colors",
                step > s.n ? "bg-emerald-400" : "bg-slate-200"
              )} />
            )}
          </div>
        ))}
      </div>

      {/* Card */}
      <div className="bg-white rounded-3xl shadow-xl border border-slate-100 w-full max-w-lg p-8">

        {/* ── Step 1 ── */}
        {step === 1 && (
          <div>
            <h1 className="text-2xl font-bold text-slate-900 mb-1">Bienvenue sur AuditBridge</h1>
            <p className="text-slate-500 text-sm mb-8">Configurons votre espace de travail en 4 étapes.</p>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Nom de votre organisation</label>
                <Input
                  placeholder="Ex: Qualité & Conformité SAS"
                  value={org.name}
                  onChange={e => setOrg(o => ({ ...o, name: e.target.value }))}
                  onKeyDown={e => e.key === "Enter" && handleOrgNext()}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Secteur d&apos;activité</label>
                <div className="grid grid-cols-2 gap-2">
                  {INDUSTRIES.map(ind => (
                    <button
                      key={ind}
                      onClick={() => setOrg(o => ({ ...o, industry: ind }))}
                      className={cn(
                        "text-left px-3 py-2 rounded-lg border text-sm transition-colors",
                        org.industry === ind
                          ? "border-blue-500 bg-blue-50 text-blue-700 font-medium"
                          : "border-slate-200 text-slate-600 hover:border-slate-300"
                      )}
                    >
                      {ind}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Taille de l&apos;équipe</label>
                <div className="grid grid-cols-2 gap-2">
                  {SIZES.map(s => (
                    <button
                      key={s.value}
                      onClick={() => setOrg(o => ({ ...o, size: s.value }))}
                      className={cn(
                        "text-left px-3 py-2 rounded-lg border text-sm transition-colors",
                        org.size === s.value
                          ? "border-blue-500 bg-blue-50 text-blue-700 font-medium"
                          : "border-slate-200 text-slate-600 hover:border-slate-300"
                      )}
                    >
                      {s.label}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <Button className="w-full mt-8" onClick={handleOrgNext} disabled={loading}>
              {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
              Continuer <ArrowRight className="h-4 w-4 ml-2" />
            </Button>
          </div>
        )}

        {/* ── Step 2 ── */}
        {step === 2 && (
          <div>
            <h1 className="text-2xl font-bold text-slate-900 mb-1">Choisissez vos référentiels</h1>
            <p className="text-slate-500 text-sm mb-8">Sélectionnez les standards que vous auditez. Vous pourrez en ajouter d&apos;autres plus tard.</p>

            <div className="space-y-2">
              {SUGGESTED_REFS.map(ref => {
                const selected = selectedRefIds.includes(ref.id);
                return (
                  <button
                    key={ref.id}
                    onClick={() => setSelectedRefIds(prev =>
                      selected ? prev.filter(id => id !== ref.id) : [...prev, ref.id]
                    )}
                    className={cn(
                      "w-full flex items-center gap-3 px-4 py-3 rounded-xl border-2 text-left transition-all",
                      selected
                        ? "border-blue-500 bg-blue-50"
                        : "border-slate-200 hover:border-slate-300 bg-white"
                    )}
                  >
                    <div className={cn(
                      "h-8 w-8 rounded-lg flex items-center justify-center shrink-0",
                      selected ? "bg-blue-600" : "bg-slate-100"
                    )}>
                      <BookOpen className={cn("h-4 w-4", selected ? "text-white" : "text-slate-500")} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className={cn("text-sm font-medium", selected ? "text-blue-800" : "text-slate-800")}>{ref.name}</p>
                      <p className="text-xs text-slate-400">{ref.category}</p>
                    </div>
                    {selected && <CheckCircle2 className="h-4 w-4 text-blue-600 shrink-0" />}
                  </button>
                );
              })}
            </div>

            <div className="flex gap-3 mt-8">
              <Button variant="outline" onClick={() => setStep(1)} className="gap-2">
                <ArrowLeft className="h-4 w-4" /> Retour
              </Button>
              <Button className="flex-1" onClick={handleRefsNext} disabled={loading}>
                {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                Importer ({selectedRefIds.length}) <ArrowRight className="h-4 w-4 ml-2" />
              </Button>
            </div>
          </div>
        )}

        {/* ── Step 3 ── */}
        {step === 3 && (
          <div>
            <h1 className="text-2xl font-bold text-slate-900 mb-1">Planifiez votre premier audit</h1>
            <p className="text-slate-500 text-sm mb-8">Créez votre premier audit maintenant — vous pouvez l&apos;ajuster ensuite.</p>

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Titre de l&apos;audit</label>
                <Input
                  placeholder="Ex: Audit ISO 9001 — Site Lille — T1 2026"
                  value={auditForm.title}
                  onChange={e => setAuditForm(f => ({ ...f, title: e.target.value }))}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">Date limite (optionnel)</label>
                <Input
                  type="date"
                  value={auditForm.dueDate}
                  onChange={e => setAuditForm(f => ({ ...f, dueDate: e.target.value }))}
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1.5">
                  Email du client / auditée <span className="text-slate-400 font-normal">(optionnel)</span>
                </label>
                <Input
                  type="email"
                  placeholder="contact@entreprise.fr"
                  value={auditForm.clientEmail}
                  onChange={e => setAuditForm(f => ({ ...f, clientEmail: e.target.value }))}
                />
                <p className="text-xs text-slate-400 mt-1">Un portail de réponse sera créé automatiquement</p>
              </div>
            </div>

            <div className="flex gap-3 mt-8">
              <Button variant="outline" onClick={() => setStep(2)} className="gap-2">
                <ArrowLeft className="h-4 w-4" /> Retour
              </Button>
              <Button className="flex-1" onClick={handleAuditNext} disabled={loading}>
                {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
                Créer l&apos;audit <ArrowRight className="h-4 w-4 ml-2" />
              </Button>
            </div>

            <button
              onClick={() => setStep(4)}
              className="w-full mt-3 text-sm text-slate-400 hover:text-slate-600 transition-colors"
            >
              Passer cette étape →
            </button>
          </div>
        )}

        {/* ── Step 4 ── */}
        {step === 4 && (
          <div>
            <h1 className="text-2xl font-bold text-slate-900 mb-1">Invitez votre équipe</h1>
            <p className="text-slate-500 text-sm mb-8">
              Ajoutez vos collègues auditeurs. Ils recevront un email d&apos;invitation.
            </p>

            <div className="flex gap-2 mb-4">
              <Input
                type="email"
                placeholder="colleague@example.com"
                value={inviteEmail}
                onChange={e => setInviteEmail(e.target.value)}
                onKeyDown={e => e.key === "Enter" && handleInvite()}
                className="flex-1"
              />
              <Button variant="outline" onClick={handleInvite} disabled={!inviteEmail.trim()}>
                Inviter
              </Button>
            </div>

            {invited.length > 0 && (
              <div className="space-y-1.5 mb-6">
                {invited.map(email => (
                  <div key={email} className="flex items-center gap-2 text-sm text-slate-600 bg-emerald-50 px-3 py-2 rounded-lg">
                    <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                    {email}
                  </div>
                ))}
              </div>
            )}

            {/* Success illustration */}
            <div className="bg-gradient-to-br from-blue-50 to-slate-50 rounded-2xl p-6 text-center mb-6 mt-4">
              <div className="h-12 w-12 bg-emerald-500 rounded-full flex items-center justify-center mx-auto mb-3">
                <CheckCircle2 className="h-6 w-6 text-white" />
              </div>
              <p className="font-semibold text-slate-800 mb-1">Configuration terminée !</p>
              <p className="text-sm text-slate-500">Votre espace AuditBridge est prêt. Vous pouvez maintenant lancer votre premier audit.</p>
            </div>

            <Button className="w-full" onClick={handleFinish}>
              Accéder au dashboard <ArrowRight className="h-4 w-4 ml-2" />
            </Button>
          </div>
        )}
      </div>

      <p className="mt-6 text-xs text-slate-400">
        Vous pourrez modifier ces paramètres à tout moment dans les réglages.
      </p>
    </div>
  );
}
