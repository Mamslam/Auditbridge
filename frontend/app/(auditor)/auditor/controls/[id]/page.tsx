"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import {
  ArrowLeft, GitBranch, Plus, Trash2, Save, ExternalLink, BarChart2,
} from "lucide-react";
import { controlsApi } from "@/lib/api/controls";
import { referentialsApi } from "@/lib/api/referentials";
import type { ControlDetail, Referential } from "@/lib/types";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

const CATEGORIES = [
  { value: "access_control", label: "Contrôle d'accès" },
  { value: "data_protection", label: "Protection des données" },
  { value: "physical", label: "Physique & environnement" },
  { value: "organizational", label: "Organisationnel" },
  { value: "technical", label: "Technique" },
  { value: "legal", label: "Légal & conformité" },
];

const STATUS_OPTIONS = ["draft", "active", "retired"] as const;
const STATUS_LABELS: Record<string, string> = {
  draft: "Brouillon", active: "Actif", retired: "Retiré",
};

export default function ControlDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const [control, setControl] = useState<ControlDetail | null>(null);
  const [referentials, setReferentials] = useState<Referential[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [form, setForm] = useState({ title: "", description: "", category: "", owner: "", status: "draft" });
  const [newMapping, setNewMapping] = useState({ referentialId: "", notes: "" });
  const [addingMapping, setAddingMapping] = useState(false);

  useEffect(() => {
    Promise.all([controlsApi.getById(id), referentialsApi.getAll()])
      .then(([ctrl, refs]) => {
        setControl(ctrl);
        setReferentials(refs);
        setForm({
          title: ctrl.title,
          description: ctrl.description ?? "",
          category: ctrl.category ?? "",
          owner: ctrl.owner ?? "",
          status: ctrl.status,
        });
      })
      .finally(() => setLoading(false));
  }, [id]);

  const handleSave = async () => {
    setSaving(true);
    try {
      const updated = await controlsApi.update(id, {
        title: form.title,
        description: form.description || undefined,
        category: form.category || undefined,
        owner: form.owner || undefined,
        status: form.status,
      });
      setControl(updated);
      toast.success("Contrôle mis à jour");
    } catch {
      toast.error("Erreur lors de la mise à jour");
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await controlsApi.delete(id);
      toast.success("Contrôle supprimé");
      router.push("/auditor/controls");
    } catch {
      toast.error("Erreur lors de la suppression");
      setDeleting(false);
    }
  };

  const handleAddMapping = async () => {
    if (!newMapping.referentialId) return;
    setAddingMapping(true);
    try {
      await controlsApi.addMapping(id, {
        referentialId: newMapping.referentialId,
        notes: newMapping.notes || undefined,
      });
      const updated = await controlsApi.getById(id);
      setControl(updated);
      setNewMapping({ referentialId: "", notes: "" });
      toast.success("Mapping ajouté");
    } catch {
      toast.error("Erreur lors de l'ajout du mapping");
    } finally {
      setAddingMapping(false);
    }
  };

  const handleRemoveMapping = async (mappingId: string) => {
    try {
      await controlsApi.removeMapping(id, mappingId);
      setControl((prev) =>
        prev ? { ...prev, mappings: prev.mappings.filter((m) => m.id !== mappingId) } : prev
      );
      toast.success("Mapping supprimé");
    } catch {
      toast.error("Erreur lors de la suppression du mapping");
    }
  };

  if (loading) return <div className="p-8 text-slate-400">Chargement...</div>;
  if (!control) return <div className="p-8 text-red-500">Contrôle introuvable</div>;

  const mappedRefIds = new Set(control.mappings.map((m) => m.referentialId));
  const availableRefs = referentials.filter((r) => !mappedRefIds.has(r.id));

  return (
    <div className="p-8 max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Link href="/auditor/controls" className="text-slate-400 hover:text-slate-600">
            <ArrowLeft className="h-5 w-5" />
          </Link>
          <div>
            <div className="flex items-center gap-2">
              <span className="font-mono text-sm font-bold text-blue-700 bg-blue-50 px-2 py-0.5 rounded">
                {control.code}
              </span>
              <h1 className="text-xl font-bold text-slate-900">{control.title}</h1>
            </div>
            <p className="text-sm text-slate-500 mt-0.5">
              {control.mappings.length} référentiel{control.mappings.length !== 1 ? "s" : ""} mappé{control.mappings.length !== 1 ? "s" : ""}
            </p>
          </div>
        </div>
        <button
          onClick={handleDelete}
          disabled={deleting}
          className="text-sm text-red-500 hover:text-red-700 hover:bg-red-50 px-3 py-1.5 rounded-lg transition-colors disabled:opacity-50"
        >
          {deleting ? "Suppression..." : "Supprimer"}
        </button>
      </div>

      <div className="grid grid-cols-3 gap-6">
        {/* Edit form */}
        <div className="col-span-2 bg-white border border-slate-200 rounded-2xl p-6 space-y-4">
          <h2 className="font-semibold text-slate-800 text-sm">Informations</h2>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Statut</label>
              <select
                value={form.status}
                onChange={(e) => setForm({ ...form, status: e.target.value })}
                className="w-full px-3 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
              >
                {STATUS_OPTIONS.map((s) => (
                  <option key={s} value={s}>{STATUS_LABELS[s]}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-600 mb-1">Catégorie</label>
              <select
                value={form.category}
                onChange={(e) => setForm({ ...form, category: e.target.value })}
                className="w-full px-3 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
              >
                <option value="">— Aucune —</option>
                {CATEGORIES.map((c) => (
                  <option key={c.value} value={c.value}>{c.label}</option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Titre</label>
            <input
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              className="w-full px-3 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Description</label>
            <textarea
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              rows={3}
              className="w-full px-3 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">Responsable</label>
            <input
              value={form.owner}
              onChange={(e) => setForm({ ...form, owner: e.target.value })}
              placeholder="email ou nom"
              className="w-full px-3 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <button
            onClick={handleSave}
            disabled={saving}
            className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-xl text-sm font-semibold hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            <Save className="h-4 w-4" />
            {saving ? "Enregistrement..." : "Enregistrer"}
          </button>
        </div>

        {/* Stats */}
        <div className="space-y-4">
          <div className="bg-white border border-slate-200 rounded-2xl p-5">
            <div className="flex items-center gap-2 mb-3">
              <GitBranch className="h-4 w-4 text-slate-400" />
              <span className="text-sm font-semibold text-slate-700">Référentiels couverts</span>
            </div>
            <p className="text-3xl font-bold text-slate-900">{control.mappings.length}</p>
            <p className="text-xs text-slate-400 mt-1">mappings actifs</p>
          </div>
          <div className="bg-blue-50 border border-blue-100 rounded-2xl p-4">
            <p className="text-xs text-blue-700 font-medium mb-1">Analyse de couverture</p>
            <p className="text-xs text-blue-600">
              Visualisez combien de questions de chaque référentiel sont couvertes par vos contrôles.
            </p>
          </div>
        </div>
      </div>

      {/* Mappings */}
      <div className="bg-white border border-slate-200 rounded-2xl p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-semibold text-slate-800 text-sm flex items-center gap-2">
            <GitBranch className="h-4 w-4" />
            Mappings référentiels
          </h2>
        </div>

        {control.mappings.length > 0 && (
          <div className="space-y-2 mb-4">
            {control.mappings.map((m) => {
              const ref = referentials.find((r) => r.id === m.referentialId);
              return (
                <div key={m.id} className="flex items-center gap-3 p-3 bg-slate-50 rounded-xl">
                  <div className="flex-1">
                    <p className="text-sm font-medium text-slate-800">
                      {ref?.name ?? m.referentialId}
                      {ref && (
                        <span className="ml-2 text-xs text-slate-400 font-normal">
                          {ref.code}
                        </span>
                      )}
                    </p>
                    {m.notes && <p className="text-xs text-slate-500 mt-0.5">{m.notes}</p>}
                  </div>
                  {ref && (
                    <Link
                      href={`/auditor/referentials/${m.referentialId}/coverage`}
                      className="text-blue-500 hover:text-blue-700 p-1"
                      title="Voir couverture"
                    >
                      <BarChart2 className="h-4 w-4" />
                    </Link>
                  )}
                  <button
                    onClick={() => handleRemoveMapping(m.id)}
                    className="text-slate-400 hover:text-red-500 p-1"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </div>
              );
            })}
          </div>
        )}

        {/* Add mapping */}
        {availableRefs.length > 0 && (
          <div className="flex gap-2 items-end">
            <div className="flex-1">
              <label className="block text-xs font-medium text-slate-600 mb-1">Ajouter un référentiel</label>
              <select
                value={newMapping.referentialId}
                onChange={(e) => setNewMapping({ ...newMapping, referentialId: e.target.value })}
                className="w-full px-3 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
              >
                <option value="">— Sélectionner —</option>
                {availableRefs.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.code} — {r.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex-1">
              <label className="block text-xs font-medium text-slate-600 mb-1">Notes (optionnel)</label>
              <input
                value={newMapping.notes}
                onChange={(e) => setNewMapping({ ...newMapping, notes: e.target.value })}
                placeholder="ex. Voir article A.8.24"
                className="w-full px-3 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <button
              onClick={handleAddMapping}
              disabled={addingMapping || !newMapping.referentialId}
              className="flex items-center gap-1.5 bg-blue-600 text-white px-4 py-2 rounded-xl text-sm font-semibold hover:bg-blue-700 disabled:opacity-50 transition-colors shrink-0"
            >
              <Plus className="h-4 w-4" />
              {addingMapping ? "..." : "Ajouter"}
            </button>
          </div>
        )}

        {control.mappings.length === 0 && availableRefs.length === 0 && (
          <p className="text-sm text-slate-400 text-center py-4">
            Tous les référentiels disponibles sont déjà mappés.
          </p>
        )}
      </div>
    </div>
  );
}
