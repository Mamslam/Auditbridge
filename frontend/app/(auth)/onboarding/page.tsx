"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
// useUser is only available when ClerkProvider is present
function useOptionalUser() {
  try {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const { useUser } = require("@clerk/nextjs");
    // eslint-disable-next-line react-hooks/rules-of-hooks
    return useUser();
  } catch {
    return { user: null };
  }
}
import { toast } from "sonner";
import { Building2, ClipboardCheck, ArrowRight, ArrowLeft, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";
import type { OrganizationType } from "@/lib/types";

type OnboardingStep = 1 | 2 | 3;

const STEPS = [
  { id: 1, label: "Profil" },
  { id: 2, label: "Organisation" },
  { id: 3, label: "Équipe" },
];

const COUNTRIES = [
  { code: "FR", label: "France" },
  { code: "DE", label: "Allemagne" },
  { code: "BE", label: "Belgique" },
  { code: "ES", label: "Espagne" },
  { code: "NL", label: "Pays-Bas" },
  { code: "IT", label: "Italie" },
  { code: "CH", label: "Suisse" },
  { code: "LU", label: "Luxembourg" },
];

const LANGUAGES = [
  { code: "fr", label: "Français" },
  { code: "en", label: "English" },
  { code: "de", label: "Deutsch" },
];

export default function OnboardingPage() {
  const router = useRouter();
  const { user } = useOptionalUser();

  const [step, setStep] = useState<OnboardingStep>(1);
  const [orgType, setOrgType] = useState<OrganizationType | null>(null);
  const [orgName, setOrgName] = useState("");
  const [country, setCountry] = useState("FR");
  const [language, setLanguage] = useState("fr");
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState("");
  const [invites, setInvites] = useState<Array<{ email: string; role: string }>>([]);
  const [isLoading, setIsLoading] = useState(false);

  const handleAddInvite = () => {
    if (!inviteEmail || !inviteRole) return;
    setInvites((prev) => [...prev, { email: inviteEmail, role: inviteRole }]);
    setInviteEmail("");
    setInviteRole("");
  };

  const handleRemoveInvite = (index: number) => {
    setInvites((prev) => prev.filter((_, i) => i !== index));
  };

  const handleComplete = async () => {
    if (!orgType || !orgName || !country) return;
    setIsLoading(true);

    try {
      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/organizations`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            name: orgName,
            type: orgType,
            countryCode: country,
            language,
            ownerClerkId: user?.id,
            ownerEmail: user?.primaryEmailAddress?.emailAddress,
            ownerFullName: user?.fullName,
            invites,
          }),
        },
      );

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.message ?? "Erreur lors de la création");
      }

      toast.success("Organisation créée avec succès !");
      router.push(
        orgType === "auditor" ? "/auditor/dashboard" : "/client/dashboard",
      );
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : "Une erreur est survenue",
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 flex items-center justify-center p-4">
      <div className="w-full max-w-2xl">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center gap-2 mb-4">
            <div className="h-8 w-8 rounded-lg bg-blue-600 flex items-center justify-center">
              <ClipboardCheck className="h-4 w-4 text-white" />
            </div>
            <span className="font-bold text-xl text-slate-900">AuditBridge</span>
          </div>
          <h1 className="text-3xl font-bold text-slate-900">
            Bienvenue sur AuditBridge
          </h1>
          <p className="text-slate-500 mt-2">
            Configurez votre espace en quelques étapes
          </p>
        </div>

        {/* Steps indicator */}
        <div className="flex items-center justify-center mb-8">
          {STEPS.map((s, index) => (
            <div key={s.id} className="flex items-center">
              <div
                className={cn(
                  "flex items-center justify-center w-8 h-8 rounded-full text-sm font-medium transition-colors",
                  step > s.id
                    ? "bg-blue-600 text-white"
                    : step === s.id
                      ? "bg-blue-600 text-white"
                      : "bg-slate-200 text-slate-500",
                )}
              >
                {step > s.id ? <Check className="h-4 w-4" /> : s.id}
              </div>
              <span
                className={cn(
                  "ml-2 text-sm font-medium",
                  step >= s.id ? "text-slate-900" : "text-slate-400",
                )}
              >
                {s.label}
              </span>
              {index < STEPS.length - 1 && (
                <div
                  className={cn(
                    "w-16 h-px mx-4",
                    step > s.id ? "bg-blue-600" : "bg-slate-200",
                  )}
                />
              )}
            </div>
          ))}
        </div>

        {/* Card */}
        <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-8">
          {/* Step 1: Choose role */}
          {step === 1 && (
            <div className="space-y-6">
              <div>
                <h2 className="text-xl font-semibold text-slate-900">
                  Quel est votre rôle ?
                </h2>
                <p className="text-slate-500 text-sm mt-1">
                  Choisissez le profil qui correspond à votre activité
                </p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <button
                  onClick={() => setOrgType("auditor")}
                  className={cn(
                    "relative p-6 rounded-xl border-2 text-left transition-all hover:border-blue-400 hover:shadow-sm",
                    orgType === "auditor"
                      ? "border-blue-600 bg-blue-50"
                      : "border-slate-200",
                  )}
                >
                  {orgType === "auditor" && (
                    <div className="absolute top-3 right-3 h-5 w-5 rounded-full bg-blue-600 flex items-center justify-center">
                      <Check className="h-3 w-3 text-white" />
                    </div>
                  )}
                  <div className="h-12 w-12 rounded-xl bg-blue-100 flex items-center justify-center mb-4">
                    <ClipboardCheck className="h-6 w-6 text-blue-600" />
                  </div>
                  <h3 className="font-semibold text-slate-900 mb-2">
                    Auditeur / Cabinet
                  </h3>
                  <p className="text-sm text-slate-500">
                    Créez des audits, gérez vos clients et générez des rapports
                    professionnels avec l'IA.
                  </p>
                  <div className="mt-4 flex flex-wrap gap-1">
                    {["GMP", "ISO", "NIS2", "RGPD"].map((tag) => (
                      <span
                        key={tag}
                        className="text-xs bg-blue-100 text-blue-700 px-2 py-0.5 rounded-full"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                </button>

                <button
                  onClick={() => setOrgType("client")}
                  className={cn(
                    "relative p-6 rounded-xl border-2 text-left transition-all hover:border-emerald-400 hover:shadow-sm",
                    orgType === "client"
                      ? "border-emerald-600 bg-emerald-50"
                      : "border-slate-200",
                  )}
                >
                  {orgType === "client" && (
                    <div className="absolute top-3 right-3 h-5 w-5 rounded-full bg-emerald-600 flex items-center justify-center">
                      <Check className="h-3 w-3 text-white" />
                    </div>
                  )}
                  <div className="h-12 w-12 rounded-xl bg-emerald-100 flex items-center justify-center mb-4">
                    <Building2 className="h-6 w-6 text-emerald-600" />
                  </div>
                  <h3 className="font-semibold text-slate-900 mb-2">
                    Entreprise cliente
                  </h3>
                  <p className="text-sm text-slate-500">
                    Répondez aux audits, soumettez vos preuves et suivez vos
                    actions correctives (CAPA).
                  </p>
                  <div className="mt-4 flex flex-wrap gap-1">
                    {["Preuves", "CAPA", "Conformité", "Rapports"].map((tag) => (
                      <span
                        key={tag}
                        className="text-xs bg-emerald-100 text-emerald-700 px-2 py-0.5 rounded-full"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                </button>
              </div>

              <Button
                onClick={() => setStep(2)}
                disabled={!orgType}
                className="w-full"
              >
                Continuer
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            </div>
          )}

          {/* Step 2: Organization info */}
          {step === 2 && (
            <div className="space-y-6">
              <div>
                <h2 className="text-xl font-semibold text-slate-900">
                  Votre organisation
                </h2>
                <p className="text-slate-500 text-sm mt-1">
                  Ces informations apparaîtront dans vos rapports d'audit
                </p>
              </div>

              <div className="space-y-4">
                <div>
                  <Label htmlFor="orgName">
                    Nom de l'organisation <span className="text-red-500">*</span>
                  </Label>
                  <Input
                    id="orgName"
                    value={orgName}
                    onChange={(e) => setOrgName(e.target.value)}
                    placeholder={
                      orgType === "auditor"
                        ? "ex: Cabinet Dupont & Associés"
                        : "ex: PharmaCorp SAS"
                    }
                    className="mt-1.5"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <Label htmlFor="country">Pays</Label>
                    <Select value={country} onValueChange={(v) => v && setCountry(v)}>
                      <SelectTrigger id="country" className="mt-1.5">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {COUNTRIES.map((c) => (
                          <SelectItem key={c.code} value={c.code}>
                            {c.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div>
                    <Label htmlFor="language">Langue de travail</Label>
                    <Select value={language} onValueChange={(v) => v && setLanguage(v)}>
                      <SelectTrigger id="language" className="mt-1.5">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {LANGUAGES.map((l) => (
                          <SelectItem key={l.code} value={l.code}>
                            {l.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              </div>

              <div className="flex gap-3">
                <Button
                  variant="outline"
                  onClick={() => setStep(1)}
                  className="flex-1"
                >
                  <ArrowLeft className="mr-2 h-4 w-4" />
                  Retour
                </Button>
                <Button
                  onClick={() => setStep(3)}
                  disabled={!orgName.trim()}
                  className="flex-1"
                >
                  Continuer
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Button>
              </div>
            </div>
          )}

          {/* Step 3: Invite team */}
          {step === 3 && (
            <div className="space-y-6">
              <div>
                <h2 className="text-xl font-semibold text-slate-900">
                  Inviter votre équipe
                </h2>
                <p className="text-slate-500 text-sm mt-1">
                  Optionnel — vous pourrez inviter des membres plus tard
                </p>
              </div>

              <div className="space-y-3">
                <div className="flex gap-2">
                  <Input
                    value={inviteEmail}
                    onChange={(e) => setInviteEmail(e.target.value)}
                    placeholder="email@exemple.com"
                    type="email"
                    className="flex-1"
                    onKeyDown={(e) => e.key === "Enter" && handleAddInvite()}
                  />
                  <Select value={inviteRole} onValueChange={(v) => v && setInviteRole(v)}>
                    <SelectTrigger className="w-48">
                      <SelectValue placeholder="Rôle" />
                    </SelectTrigger>
                    <SelectContent>
                      {orgType === "auditor" ? (
                        <>
                          <SelectItem value="auditor_junior">Auditeur junior</SelectItem>
                          <SelectItem value="auditor_viewer">Observateur</SelectItem>
                        </>
                      ) : (
                        <>
                          <SelectItem value="client_contributor">Contributeur</SelectItem>
                          <SelectItem value="client_viewer">Lecteur</SelectItem>
                        </>
                      )}
                    </SelectContent>
                  </Select>
                  <Button
                    variant="outline"
                    onClick={handleAddInvite}
                    disabled={!inviteEmail || !inviteRole}
                  >
                    Ajouter
                  </Button>
                </div>

                {invites.length > 0 && (
                  <div className="space-y-2">
                    {invites.map((invite, index) => (
                      <div
                        key={index}
                        className="flex items-center justify-between bg-slate-50 rounded-lg p-3"
                      >
                        <div>
                          <p className="text-sm font-medium text-slate-900">
                            {invite.email}
                          </p>
                          <p className="text-xs text-slate-500">{invite.role}</p>
                        </div>
                        <button
                          onClick={() => handleRemoveInvite(index)}
                          className="text-slate-400 hover:text-red-500 text-sm"
                        >
                          ✕
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <div className="flex gap-3">
                <Button
                  variant="outline"
                  onClick={() => setStep(2)}
                  className="flex-1"
                >
                  <ArrowLeft className="mr-2 h-4 w-4" />
                  Retour
                </Button>
                <Button
                  onClick={handleComplete}
                  disabled={isLoading}
                  className="flex-1"
                >
                  {isLoading ? (
                    "Création en cours..."
                  ) : (
                    <>
                      Accéder au tableau de bord
                      <ArrowRight className="ml-2 h-4 w-4" />
                    </>
                  )}
                </Button>
              </div>
            </div>
          )}
        </div>

        <p className="text-center text-xs text-slate-400 mt-6">
          AuditBridge — La plateforme d'audit qui connecte l'Europe
        </p>
      </div>
    </div>
  );
}
