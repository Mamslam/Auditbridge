import Link from "next/link";
import { ClipboardCheck, Building2, Shield, Zap, BarChart3, FileText } from "lucide-react";

export default function HomePage() {
  return (
    <div className="min-h-screen bg-white">
      {/* Nav */}
      <nav className="border-b border-slate-100 px-6 py-4 flex items-center justify-between">
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
          <Zap className="h-3.5 w-3.5" />
          Propulsé par Claude AI (Anthropic)
        </div>
        <h1 className="text-5xl font-bold text-slate-900 leading-tight mb-6">
          La plateforme d'audit qui<br />
          <span className="text-blue-600">connecte l'Europe</span>
        </h1>
        <p className="text-xl text-slate-500 max-w-2xl mx-auto mb-10">
          Gérez vos audits GMP, ISO 27001, NIS2, RGPD et plus encore.
          Connectez auditeurs et entreprises clientes dans un espace sécurisé
          avec analyse IA intégrée.
        </p>
        <div className="flex items-center justify-center gap-4">
          <Link
            href="/onboarding"
            className="bg-blue-600 text-white px-8 py-3.5 rounded-xl font-semibold hover:bg-blue-700 transition-colors"
          >
            Démarrer gratuitement
          </Link>
          <a
            href="#features"
            className="text-slate-600 px-8 py-3.5 rounded-xl font-semibold border border-slate-200 hover:border-slate-300 hover:bg-slate-50 transition-colors"
          >
            En savoir plus
          </a>
        </div>
      </section>

      {/* Frameworks */}
      <section className="border-y border-slate-100 py-8 bg-slate-50">
        <div className="max-w-5xl mx-auto px-6">
          <p className="text-center text-xs text-slate-400 font-medium uppercase tracking-wider mb-5">
            Référentiels supportés
          </p>
          <div className="flex flex-wrap items-center justify-center gap-6">
            {["GMP / EU GMP", "ISO 9001", "ISO 27001", "ISO 14001", "NIS2", "RGPD", "HACCP", "CSRD", "DORA"].map((f) => (
              <span key={f} className="text-sm font-semibold text-slate-600 bg-white border border-slate-200 px-3 py-1.5 rounded-lg">
                {f}
              </span>
            ))}
          </div>
        </div>
      </section>

      {/* Features */}
      <section id="features" className="max-w-5xl mx-auto px-6 py-24">
        <h2 className="text-3xl font-bold text-slate-900 text-center mb-12">
          Tout ce dont vous avez besoin
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {[
            {
              icon: <ClipboardCheck className="h-6 w-6 text-blue-600" />,
              title: "Audits structurés",
              desc: "Templates par référentiel, questions typées, marquage GMP critique, versioning.",
            },
            {
              icon: <Shield className="h-6 w-6 text-emerald-600" />,
              title: "Sécurité maximale",
              desc: "RLS PostgreSQL, URLs signées, audit trail immuable, chiffrement end-to-end.",
            },
            {
              icon: <Zap className="h-6 w-6 text-purple-600" />,
              title: "IA Anthropic Claude",
              desc: "Classification réglementaire, analyse préventive, génération de rapports PDF.",
            },
            {
              icon: <Building2 className="h-6 w-6 text-orange-600" />,
              title: "Multi-tenancy strict",
              desc: "Isolation complète des données par organisation. Aucun croisement possible.",
            },
            {
              icon: <BarChart3 className="h-6 w-6 text-rose-600" />,
              title: "Score de conformité",
              desc: "Calcul en temps réel, historique graphique, benchmarking sectoriel.",
            },
            {
              icon: <FileText className="h-6 w-6 text-slate-600" />,
              title: "CAPA automatiques",
              desc: "Actions correctives générées par l'IA, suivi des délais, clôture avec preuves.",
            },
          ].map((f) => (
            <div key={f.title} className="p-6 rounded-xl border border-slate-200 hover:border-slate-300 hover:shadow-sm transition-all">
              <div className="h-12 w-12 rounded-xl bg-slate-50 flex items-center justify-center mb-4">
                {f.icon}
              </div>
              <h3 className="font-semibold text-slate-900 mb-2">{f.title}</h3>
              <p className="text-sm text-slate-500">{f.desc}</p>
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
            className="bg-white text-blue-600 px-8 py-3.5 rounded-xl font-semibold hover:bg-blue-50 transition-colors inline-block"
          >
            Créer mon compte gratuitement
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-slate-100 py-8 px-6 text-center text-sm text-slate-400">
        AuditBridge v1.0 — La plateforme d'audit qui connecte l'Europe 🇫🇷 🇩🇪 🇧🇪 🇪🇸 🇳🇱
      </footer>
    </div>
  );
}
