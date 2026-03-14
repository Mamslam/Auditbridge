import Link from "next/link";
import { CheckCircle2, ArrowRight } from "lucide-react";

const PLANS = [
  {
    name: "Starter",
    price: "€299",
    period: "/mois",
    description: "Pour les équipes qualité qui démarrent leur programme d'audit.",
    color: "border-slate-200",
    cta: "Démarrer l'essai",
    ctaStyle: "bg-slate-900 text-white hover:bg-slate-800",
    features: [
      "5 audits actifs",
      "3 utilisateurs",
      "10 référentiels système",
      "Portail client",
      "PDF reports",
      "Stockage 5 Go",
    ],
  },
  {
    name: "Pro",
    price: "€599",
    period: "/mois",
    description: "Pour les organisations gérant plusieurs standards simultanément.",
    color: "border-blue-600 ring-2 ring-blue-600",
    badge: "Le plus populaire",
    cta: "Démarrer l'essai",
    ctaStyle: "bg-blue-600 text-white hover:bg-blue-700",
    features: [
      "Audits illimités",
      "10 utilisateurs",
      "Tous les référentiels",
      "Bibliothèque de contrôles",
      "Cartographie multi-standards",
      "IA complète (analyse, CAPA, QA)",
      "Dashboard analytique",
      "Stockage 50 Go",
    ],
  },
  {
    name: "Enterprise",
    price: "€999",
    period: "/mois",
    description: "Pour les grandes équipes avec des besoins avancés de conformité.",
    color: "border-slate-200",
    cta: "Contacter l'équipe",
    ctaHref: "mailto:enterprise@auditbridge.io",
    ctaStyle: "bg-slate-900 text-white hover:bg-slate-800",
    features: [
      "Tout de Pro",
      "Utilisateurs illimités",
      "SSO / SAML",
      "SLA 99,9%",
      "Onboarding dédié",
      "Intégrations Jira/Slack/Teams",
      "Audit trail complet",
      "Support prioritaire",
    ],
  },
];

const FAQS = [
  {
    q: "Puis-je annuler à tout moment ?",
    a: "Oui, sans engagement. Votre abonnement reste actif jusqu'à la fin de la période payée.",
  },
  {
    q: "L'essai gratuit nécessite-t-il une carte bancaire ?",
    a: "Non. 30 jours complets sans carte, sans condition.",
  },
  {
    q: "Mes données sont-elles hébergées en Europe ?",
    a: "Oui. Toutes les données sont stockées sur des serveurs européens (AWS eu-west-3, Paris).",
  },
  {
    q: "Puis-je migrer depuis un autre outil ?",
    a: "Oui. Notre équipe vous accompagne gratuitement pour l'import de vos référentiels et données existantes.",
  },
];

export default function PricingPage() {
  return (
    <main>
      {/* Hero */}
      <section className="text-center py-20 px-6">
        <p className="text-sm font-semibold text-blue-600 mb-3 uppercase tracking-wide">Tarification transparente</p>
        <h1 className="text-4xl md:text-5xl font-bold text-slate-900 mb-4">
          Des prix clairs,<br />sans mauvaises surprises
        </h1>
        <p className="text-lg text-slate-500 max-w-xl mx-auto mb-8">
          3 à 10× moins cher que AuditBoard, LogicGate ou MetricStream — pour les mêmes fonctionnalités essentielles.
        </p>
        <p className="text-sm text-emerald-600 font-medium">
          ✓ 30 jours d&apos;essai gratuit · ✓ Sans carte bancaire · ✓ Sans engagement
        </p>
      </section>

      {/* Plans */}
      <section className="max-w-5xl mx-auto px-6 pb-20">
        <div className="grid md:grid-cols-3 gap-6">
          {PLANS.map((plan) => (
            <div
              key={plan.name}
              className={`relative rounded-2xl border-2 p-7 flex flex-col ${plan.color}`}
            >
              {plan.badge && (
                <span className="absolute -top-3 left-1/2 -translate-x-1/2 bg-blue-600 text-white text-xs font-bold px-3 py-1 rounded-full">
                  {plan.badge}
                </span>
              )}
              <div className="mb-6">
                <h2 className="text-lg font-bold text-slate-900 mb-1">{plan.name}</h2>
                <p className="text-slate-500 text-sm mb-4">{plan.description}</p>
                <div className="flex items-end gap-1">
                  <span className="text-4xl font-extrabold text-slate-900">{plan.price}</span>
                  <span className="text-slate-400 text-sm mb-1">{plan.period}</span>
                </div>
                <p className="text-xs text-slate-400 mt-1">Facturé mensuellement, HT</p>
              </div>

              <ul className="space-y-2.5 flex-1 mb-6">
                {plan.features.map((f) => (
                  <li key={f} className="flex items-start gap-2 text-sm text-slate-700">
                    <CheckCircle2 className="h-4 w-4 text-emerald-500 mt-0.5 shrink-0" />
                    {f}
                  </li>
                ))}
              </ul>

              <Link
                href={plan.ctaHref ?? "/sign-up"}
                className={`flex items-center justify-center gap-2 rounded-xl py-2.5 text-sm font-semibold transition-colors ${plan.ctaStyle}`}
              >
                {plan.cta}
                <ArrowRight className="h-4 w-4" />
              </Link>
            </div>
          ))}
        </div>

        <p className="text-center text-sm text-slate-400 mt-6">
          Tous les plans incluent le support email. Les prix s&apos;entendent hors taxes.
        </p>
      </section>

      {/* Comparison callout */}
      <section className="bg-slate-50 py-16 px-6">
        <div className="max-w-3xl mx-auto text-center">
          <h2 className="text-2xl font-bold text-slate-900 mb-4">Comparez avec la concurrence</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-200">
                  <th className="py-3 pr-6 text-slate-600 font-semibold">Outil</th>
                  <th className="py-3 pr-6 text-slate-600 font-semibold">Prix /an</th>
                  <th className="py-3 pr-6 text-slate-600 font-semibold">IA native</th>
                  <th className="py-3 pr-6 text-slate-600 font-semibold">Portail client</th>
                  <th className="py-3 text-slate-600 font-semibold">Multi-standards</th>
                </tr>
              </thead>
              <tbody>
                {[
                  { name: "AuditBoard", price: "€40K–€150K", ai: "✓", portal: "✓", multi: "✓" },
                  { name: "LogicGate", price: "€14K–€130K", ai: "✗", portal: "~", multi: "~" },
                  { name: "Resolver", price: "~€25K", ai: "✗", portal: "✗", multi: "~" },
                  { name: "SafetyCulture", price: "Gratuit–€29/user", ai: "✗", portal: "✗", multi: "✗" },
                  { name: "AuditBridge Pro ✨", price: "€7K", ai: "✓", portal: "✓", multi: "✓", highlight: true },
                ].map((row) => (
                  <tr key={row.name} className={`border-b border-slate-100 ${row.highlight ? "bg-blue-50 font-semibold" : ""}`}>
                    <td className="py-3 pr-6 text-slate-800">{row.name}</td>
                    <td className="py-3 pr-6 text-slate-700">{row.price}</td>
                    <td className={`py-3 pr-6 ${row.ai === "✓" ? "text-emerald-600" : "text-slate-400"}`}>{row.ai}</td>
                    <td className={`py-3 pr-6 ${row.portal === "✓" ? "text-emerald-600" : "text-slate-400"}`}>{row.portal}</td>
                    <td className={`py-3 ${row.multi === "✓" ? "text-emerald-600" : "text-slate-400"}`}>{row.multi}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </section>

      {/* FAQ */}
      <section className="max-w-2xl mx-auto px-6 py-16">
        <h2 className="text-2xl font-bold text-slate-900 mb-8 text-center">Questions fréquentes</h2>
        <div className="space-y-6">
          {FAQS.map((faq) => (
            <div key={faq.q} className="border-b border-slate-100 pb-6">
              <h3 className="font-semibold text-slate-800 mb-2">{faq.q}</h3>
              <p className="text-slate-500 text-sm">{faq.a}</p>
            </div>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="bg-blue-600 py-16 px-6 text-center text-white">
        <h2 className="text-3xl font-bold mb-3">Prêt à moderniser vos audits ?</h2>
        <p className="text-blue-100 mb-8 max-w-lg mx-auto">
          Lancez votre premier audit en moins de 30 minutes. Aucune carte bancaire requise.
        </p>
        <Link
          href="/sign-up"
          className="inline-flex items-center gap-2 bg-white text-blue-600 font-bold px-8 py-3 rounded-xl hover:bg-blue-50 transition-colors text-lg"
        >
          Démarrer gratuitement <ArrowRight className="h-5 w-5" />
        </Link>
      </section>
    </main>
  );
}
