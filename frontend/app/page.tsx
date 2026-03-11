import Link from "next/link";
import {
  ClipboardCheck, Building2, Shield, Zap, BarChart3, FileText,
  Globe, ArrowRight, CheckCircle2
} from "lucide-react";

const NORMS = [
  "EU GMP", "ISO 9001", "ISO 14001", "ISO 27001", "ISO 45001", "ISO 50001",
  "NIS2", "RGPD / GDPR", "HACCP", "IFS Food", "BRC / BRCGS", "CSRD",
  "DORA", "SOC 2", "HDS", "ISO 13485", "MDR", "EASA Part-145",
  "SMETA", "SA8000", "GxP", "FDA 21 CFR", "ICH Q10", "REACH",
];

const REFERENTIAL_GRID = [
  { icon: "💊", name: "EU GMP / BPF",   desc: "Bonnes Pratiques de Fabrication pharmaceutiques",    cat: "Pharma" },
  { icon: "✅", name: "ISO 9001:2015",   desc: "Systèmes de management de la qualité",                cat: "Qualité" },
  { icon: "🔒", name: "ISO 27001:2022",  desc: "Sécurité de l'information",                          cat: "Cyber" },
  { icon: "🛡️", name: "RGPD",            desc: "Règlement Général sur la Protection des Données",    cat: "Données" },
  { icon: "🌾", name: "HACCP",           desc: "Analyse des risques et points critiques",             cat: "Alimentaire" },
  { icon: "🌿", name: "ISO 14001",       desc: "Systèmes de management environnemental",              cat: "Environnement" },
  { icon: "⚡", name: "NIS2",            desc: "Directive sécurité des réseaux et systèmes d'information", cat: "Cyber" },
  { icon: "🏦", name: "DORA",            desc: "Digital Operational Resilience Act",                  cat: "Finance" },
  { icon: "🌱", name: "CSRD / DPEF",     desc: "Reporting de durabilité des entreprises",             cat: "RSE" },
  { icon: "🏥", name: "ISO 13485",       desc: "Dispositifs médicaux — SMQ",                          cat: "Médical" },
  { icon: "🔐", name: "SOC 2",           desc: "Sécurité, disponibilité, confidentialité",            cat: "Cyber" },
  { icon: "🍽️", name: "IFS Food",        desc: "International Featured Standards — Alimentation",    cat: "Alimentaire" },
];

export default function HomePage() {
  return (
    <div className="min-h-screen bg-white">
      {/* Nav */}
      <nav className="border-b border-slate-100 px-6 py-4 flex items-center justify-between sticky top-0 bg-white/95 backdrop-blur z-10">
        <div className="flex items-center gap-2">
          <div className="h-8 w-8 rounded-lg bg-blue-600 flex items-center justify-center">
            <ClipboardCheck className="h-4 w-4 text-white" />
          </div>
          <span className="font-bold text-xl text-slate-900">AuditBridge</span>
        </div>
        <div className="flex items-center gap-4">
          <Link href="/sign-in" className="text-sm text-slate-600 hover:text-slate-900">
            Se connecter
          </Link>
          <Link
            href="/onboarding"
            className="text-sm bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
          >
            Commencer gratuitement
          </Link>
        </div>
      </nav>

      {/* Hero */}
      <section className="max-w-5xl mx-auto px-6 pt-24 pb-16 text-center">
        <div className="inline-flex items-center gap-2 bg-blue-50 text-blue-700 text-sm font-medium px-3 py-1.5 rounded-full mb-6">
          <Globe className="h-3.5 w-3.5" />
          Moteur d'audit universel — 20+ référentiels
        </div>
        <h1 className="text-5xl font-bold text-slate-900 leading-tight mb-6">
          Auditez n'importe quel<br />
          <span className="text-blue-600">référentiel au monde</span>
        </h1>
        <p className="text-xl text-slate-500 max-w-2xl mx-auto mb-10">
          GMP, ISO, RGPD, HACCP, NIS2, DORA, CSRD... et bien plus encore.
          Un seul outil pour tous vos audits, avec l'analyse IA Claude intégrée.
        </p>
        <div className="flex items-center justify-center gap-4 flex-wrap">
          <Link
            href="/onboarding"
            className="bg-blue-600 text-white px-8 py-3.5 rounded-xl font-semibold hover:bg-blue-700 transition-colors flex items-center gap-2"
          >
            Démarrer gratuitement
            <ArrowRight className="h-4 w-4" />
          </Link>
          <a
            href="#referentials"
            className="text-slate-600 px-8 py-3.5 rounded-xl font-semibold border border-slate-200 hover:border-slate-300 hover:bg-slate-50 transition-colors"
          >
            Voir les référentiels
          </a>
        </div>
        <div className="flex items-center justify-center gap-6 mt-10 text-sm text-slate-500 flex-wrap">
          {[
            { icon: Shield, text: "RLS multi-tenant" },
            { icon: Zap, text: "IA Claude / Anthropic" },
            { icon: Globe, text: "20+ référentiels" },
            { icon: CheckCircle2, text: "Rapport automatique" },
          ].map(({ icon: Icon, text }) => (
            <div key={text} className="flex items-center gap-1.5">
              <Icon className="h-4 w-4 text-blue-500" />
              {text}
            </div>
          ))}
        </div>
      </section>

      {/* Norms ticker */}
      <section className="border-y border-slate-100 py-5 bg-slate-50 overflow-hidden">
        <div className="flex gap-3" style={{ animation: "scroll 40s linear infinite" }}>
          {[...NORMS, ...NORMS].map((norm, i) => (
            <span
              key={i}
              className="inline-flex items-center text-sm font-semibold text-slate-600 bg-white border border-slate-200 px-3 py-1.5 rounded-lg shrink-0 whitespace-nowrap"
            >
              {norm}
            </span>
          ))}
        </div>
      </section>

      {/* Referentials grid */}
      <section id="referentials" className="max-w-5xl mx-auto px-6 py-24">
        <div className="text-center mb-12">
          <h2 className="text-3xl font-bold text-slate-900 mb-3">
            Référentiels inclus dès le premier jour
          </h2>
          <p className="text-slate-500">
            Tous les templates sont structurés, versionnés, et éditables par votre équipe.
          </p>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {REFERENTIAL_GRID.map((ref) => (
            <div
              key={ref.name}
              className="flex items-start gap-3.5 p-4 rounded-2xl border border-slate-200 bg-white hover:border-slate-300 hover:shadow-sm transition-all"
            >
              <div className="h-10 w-10 rounded-xl bg-slate-50 flex items-center justify-center text-xl shrink-0">
                {ref.icon}
              </div>
              <div className="min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <h3 className="font-semibold text-slate-900 text-sm">{ref.name}</h3>
                  <span className="text-[10px] text-slate-400 bg-slate-100 px-1.5 py-0.5 rounded font-medium shrink-0">
                    {ref.cat}
                  </span>
                </div>
                <p className="text-xs text-slate-500 mt-0.5 leading-relaxed">{ref.desc}</p>
              </div>
            </div>
          ))}
        </div>
        <p className="text-center text-sm text-slate-400 mt-8">
          + Créez vos propres référentiels personnalisés en quelques minutes
        </p>
      </section>

      {/* Features */}
      <section className="bg-slate-50 border-y border-slate-100 py-24 px-6">
        <div className="max-w-5xl mx-auto">
          <h2 className="text-3xl font-bold text-slate-900 text-center mb-12">
            Tout ce dont vous avez besoin
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {[
              {
                icon: <ClipboardCheck className="h-6 w-6 text-blue-600" />,
                title: "Éditeur de templates",
                desc: "Créez et organisez vos sections et questions. Versionnez chaque référentiel.",
              },
              {
                icon: <Shield className="h-6 w-6 text-emerald-600" />,
                title: "Isolation totale des données",
                desc: "RLS PostgreSQL. Chaque organisation ne voit que ses données. Zero trust par défaut.",
              },
              {
                icon: <Zap className="h-6 w-6 text-purple-600" />,
                title: "IA Claude intégrée",
                desc: "Classification réglementaire, analyse préventive, génération de rapport narratif automatique.",
              },
              {
                icon: <Building2 className="h-6 w-6 text-orange-600" />,
                title: "Portail client sans compte",
                desc: "Envoyez un lien unique au client. Il répond directement sans créer de compte.",
              },
              {
                icon: <BarChart3 className="h-6 w-6 text-rose-600" />,
                title: "Score de conformité",
                desc: "Calcul en temps réel par question, par section et par audit. Historique graphique.",
              },
              {
                icon: <FileText className="h-6 w-6 text-slate-600" />,
                title: "CAPAs automatiques",
                desc: "Actions correctives générées par l'IA, assignées, suivies jusqu'à la clôture.",
              },
            ].map((f) => (
              <div key={f.title} className="p-6 rounded-2xl border border-slate-200 bg-white hover:border-slate-300 hover:shadow-sm transition-all">
                <div className="h-12 w-12 rounded-xl bg-slate-50 flex items-center justify-center mb-4">
                  {f.icon}
                </div>
                <h3 className="font-semibold text-slate-900 mb-2">{f.title}</h3>
                <p className="text-sm text-slate-500 leading-relaxed">{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="max-w-4xl mx-auto px-6 py-24">
        <h2 className="text-3xl font-bold text-slate-900 text-center mb-12">
          Comment ça marche
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          {[
            { step: "1", title: "Choisissez un référentiel", desc: "Parmi les 20+ disponibles ou créez le vôtre" },
            { step: "2", title: "Activez l'audit", desc: "Un lien client sécurisé est généré automatiquement" },
            { step: "3", title: "Le client répond", desc: "Directement dans le portail, sans compte requis" },
            { step: "4", title: "L'IA analyse", desc: "Rapport, CAPAs et score générés par Claude" },
          ].map((s) => (
            <div key={s.step} className="text-center">
              <div className="h-12 w-12 rounded-2xl bg-blue-600 text-white text-xl font-bold flex items-center justify-center mx-auto mb-3">
                {s.step}
              </div>
              <h3 className="font-semibold text-slate-900 mb-1 text-sm">{s.title}</h3>
              <p className="text-xs text-slate-500 leading-relaxed">{s.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="bg-blue-600 py-16 px-6">
        <div className="max-w-2xl mx-auto text-center">
          <h2 className="text-3xl font-bold text-white mb-4">
            Prêt à moderniser vos audits ?
          </h2>
          <p className="text-blue-100 mb-8">
            Rejoignez les cabinets d'audit et entreprises européens qui font confiance à AuditBridge.
          </p>
          <Link
            href="/onboarding"
            className="bg-white text-blue-600 px-8 py-3.5 rounded-xl font-semibold hover:bg-blue-50 transition-colors inline-flex items-center gap-2"
          >
            Créer mon compte gratuitement
            <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-slate-100 py-8 px-6 text-center text-sm text-slate-400">
        <div className="flex items-center justify-center gap-2 mb-2">
          <div className="h-5 w-5 rounded bg-blue-600 flex items-center justify-center">
            <ClipboardCheck className="h-3 w-3 text-white" />
          </div>
          <span className="font-semibold text-slate-600">AuditBridge</span>
        </div>
        Le moteur d'audit universel — FR · DE · BE · ES · NL · IT · CH
      </footer>
    </div>
  );
}
