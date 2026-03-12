"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, ArrowRight, Check, ClipboardCheck, Loader2 } from "lucide-react";
import { referentialsApi } from "@/lib/api/referentials";
import { auditsApi } from "@/lib/api/audits";
import type { Referential, ReferentialCategory } from "@/lib/types";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

const STEPS = [
  { id: 1, label: "Référentiel" },
  { id: 2, label: "Détails" },
  { id: 3, label: "Confirmation" },
];

export default function NewAuditPage() {
  const router = useRouter();
  const [step, setStep] = useState(1);
  const [referentials, setReferentials] = useState<Referential[]>([]);
  const [categories, setCategories] = useState<ReferentialCategory[]>([]);
  const [selectedRefId, setSelectedRefId] = useState<string | null>(null);
  const [title, setTitle] = useState("");
  const [deadline, setDeadline] = useState("");
  const [scope, setScope] = useState("");
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [activeCategory, setActiveCategory] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([referentialsApi.getAll(), referentialsApi.getCategories()])
      .then(([refs, cats]) => { setReferentials(refs); setCategories(cats); })
      .finally(() => setLoading(false));
  }, []);

  const selectedRef = referentials.find((r) => r.id === selectedRefId);

  const handleCreate = async () => {
    if (!selectedRefId || !title.trim()) return;
    setCreating(true);
    try {
      const audit = await auditsApi.create({
        referentialId: selectedRefId,
        title: title.trim(),
        dueDate: deadline || undefined,
        scope: scope || undefined,
      });
      toast.success("Audit créé avec succès");
      router.push(`/auditor/audits/${audit.id}`);
    } catch {
      toast.error("Erreur lors de la création");
      setCreating(false);
    }
  };

  const filteredRefs = referentials.filter((r) =>
    !activeCategory || r.category?.slug === activeCategory
  );

  return (
    <div className="p-8 max-w-3xl mx-auto">
      {/* Header */}
      <div className="flex items-center gap-3 mb-8">
        <button
          onClick={() => step > 1 ? setStep(step - 1) : router.back()}
          className="text-slate-400 hover:text-slate-600 transition-colors"
        >
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div>
          <h1 className="text-xl font-bold text-slate-900">Nouvel audit</h1>
          <p className="text-sm text-slate-400 mt-0.5">Étape {step}/3</p>
        </div>
      </div>

      {/* Step indicator */}
      <div className="flex items-center gap-0 mb-8">
        {STEPS.map((s, i) => (
          <div key={s.id} className="flex items-center flex-1 last:flex-none">
            <div className={cn(
              "flex items-center gap-2",
              step > s.id ? "text-blue-600" : step === s.id ? "text-blue-600" : "text-slate-400"
            )}>
              <div className={cn(
                "h-7 w-7 rounded-full flex items-center justify-center text-xs font-semibold border-2 transition-colors",
                step > s.id ? "bg-blue-600 border-blue-600 text-white" :
                step === s.id ? "border-blue-600 text-blue-600" :
                "border-slate-200 text-slate-400"
              )}>
                {step > s.id ? <Check className="h-3.5 w-3.5" /> : s.id}
              </div>
              <span className="text-sm font-medium hidden sm:block">{s.label}</span>
            </div>
            {i < STEPS.length - 1 && (
              <div className={cn("flex-1 h-px mx-3 transition-colors", step > s.id ? "bg-blue-600" : "bg-slate-200")} />
            )}
          </div>
        ))}
      </div>

      {/* Step 1: Choose referential */}
      {step === 1 && (
        <div className="space-y-5">
          <div>
            <h2 className="text-lg font-semibold text-slate-900 mb-1">Choisir un référentiel</h2>
            <p className="text-sm text-slate-500">Sélectionnez le standard sur lequel sera basé cet audit</p>
          </div>

          {/* Category filter */}
          <div className="flex gap-2 flex-wrap">
            <button
              onClick={() => setActiveCategory(null)}
              className={cn("px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors",
                !activeCategory ? "bg-blue-600 text-white border-blue-600" : "bg-white text-slate-600 border-slate-200"
              )}
            >Tous</button>
            {categories.map((cat) => (
              <button
                key={cat.slug}
                onClick={() => setActiveCategory(activeCategory === cat.slug ? null : cat.slug)}
                className={cn("px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors",
                  activeCategory === cat.slug ? "bg-blue-600 text-white border-blue-600" : "bg-white text-slate-600 border-slate-200"
                )}
              >
                {cat.icon} {cat.label}
              </button>
            ))}
          </div>

          {loading ? (
            <div className="grid grid-cols-2 gap-3">
              {[...Array(6)].map((_, i) => (
                <div key={i} className="h-20 bg-slate-100 rounded-2xl animate-pulse" />
              ))}
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-3 max-h-96 overflow-y-auto pr-1">
              {filteredRefs.map((ref) => (
                <button
                  key={ref.id}
                  onClick={() => {
                    setSelectedRefId(ref.id);
                    setTitle(`Audit ${ref.name} - ${new Date().getFullYear()}`);
                  }}
                  className={cn(
                    "text-left p-4 rounded-2xl border-2 transition-all hover:border-blue-300",
                    selectedRefId === ref.id
                      ? "border-blue-600 bg-blue-50"
                      : "border-slate-200 bg-white hover:shadow-sm"
                  )}
                >
                  <div className="flex items-center gap-2.5 mb-1.5">
                    <span className="text-lg">{ref.category?.icon ?? "📋"}</span>
                    <div className="min-w-0">
                      <p className="text-sm font-semibold text-slate-900 truncate">{ref.name}</p>
                      <p className="text-xs text-slate-400">{ref.version}</p>
                    </div>
                    {selectedRefId === ref.id && (
                      <div className="ml-auto h-5 w-5 rounded-full bg-blue-600 flex items-center justify-center shrink-0">
                        <Check className="h-3 w-3 text-white" />
                      </div>
                    )}
                  </div>
                </button>
              ))}
            </div>
          )}

          <button
            onClick={() => setStep(2)}
            disabled={!selectedRefId}
            className="w-full flex items-center justify-center gap-2 bg-blue-600 text-white py-3 rounded-xl font-semibold hover:bg-blue-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Continuer <ArrowRight className="h-4 w-4" />
          </button>
        </div>
      )}

      {/* Step 2: Details */}
      {step === 2 && (
        <div className="space-y-5">
          <div>
            <h2 className="text-lg font-semibold text-slate-900 mb-1">Détails de l'audit</h2>
            <p className="text-sm text-slate-500">Référentiel : <strong>{selectedRef?.name}</strong></p>
          </div>

          <FormField label="Titre de l'audit *">
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full text-sm border border-slate-200 rounded-xl px-4 py-2.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="ex: Audit GMP Site Paris Q1 2026"
            />
          </FormField>

          <FormField label="Échéance">
            <input
              type="date"
              value={deadline}
              onChange={(e) => setDeadline(e.target.value)}
              className="w-full text-sm border border-slate-200 rounded-xl px-4 py-2.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </FormField>

          <FormField label="Périmètre / scope (optionnel)">
            <textarea
              value={scope}
              onChange={(e) => setScope(e.target.value)}
              rows={3}
              placeholder="Décrivez le périmètre de l'audit..."
              className="w-full text-sm border border-slate-200 rounded-xl px-4 py-2.5 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
          </FormField>

          <div className="flex gap-3">
            <button
              onClick={() => setStep(1)}
              className="flex-1 py-3 rounded-xl border border-slate-200 text-sm font-semibold text-slate-600 hover:bg-slate-50 transition-colors"
            >
              Retour
            </button>
            <button
              onClick={() => setStep(3)}
              disabled={!title.trim()}
              className="flex-1 flex items-center justify-center gap-2 bg-blue-600 text-white py-3 rounded-xl font-semibold hover:bg-blue-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              Continuer <ArrowRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      )}

      {/* Step 3: Confirm */}
      {step === 3 && (
        <div className="space-y-5">
          <div>
            <h2 className="text-lg font-semibold text-slate-900 mb-1">Récapitulatif</h2>
            <p className="text-sm text-slate-500">Vérifiez les informations avant de créer l'audit</p>
          </div>

          <div className="bg-slate-50 rounded-2xl p-5 space-y-3">
            <Row label="Référentiel" value={`${selectedRef?.category?.icon ?? ""} ${selectedRef?.name ?? ""} ${selectedRef?.version ?? ""}`} />
            <Row label="Titre" value={title} />
            <Row label="Échéance" value={deadline ? new Date(deadline).toLocaleDateString("fr-FR") : "Non définie"} />
            {scope && <Row label="Périmètre" value={scope} />}
          </div>

          <div className="bg-blue-50 rounded-xl p-4 flex items-start gap-3">
            <ClipboardCheck className="h-5 w-5 text-blue-600 mt-0.5 shrink-0" />
            <p className="text-sm text-blue-700">
              L'audit sera créé en statut <strong>brouillon</strong>. Vous pourrez l'activer pour envoyer le lien au client.
            </p>
          </div>

          <div className="flex gap-3">
            <button
              onClick={() => setStep(2)}
              className="flex-1 py-3 rounded-xl border border-slate-200 text-sm font-semibold text-slate-600 hover:bg-slate-50 transition-colors"
            >
              Retour
            </button>
            <button
              onClick={handleCreate}
              disabled={creating}
              className="flex-1 flex items-center justify-center gap-2 bg-blue-600 text-white py-3 rounded-xl font-semibold hover:bg-blue-700 disabled:opacity-70 transition-colors"
            >
              {creating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}
              {creating ? "Création..." : "Créer l'audit"}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function FormField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <label className="text-sm font-medium text-slate-700">{label}</label>
      {children}
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-start justify-between gap-4">
      <span className="text-sm text-slate-500 shrink-0">{label}</span>
      <span className="text-sm font-medium text-slate-900 text-right">{value}</span>
    </div>
  );
}
