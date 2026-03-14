"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Search, BookOpen, ArrowRight, CheckCircle2, Tag } from "lucide-react";

interface PublicReferential {
  id: string;
  name: string;
  description?: string;
  category?: string;
  sectionCount: number;
  questionCount: number;
  isSystem: boolean;
}

const CATEGORY_ORDER = [
  "Pharmaceutique",
  "Qualité",
  "Cybersécurité",
  "Alimentaire",
  "Environnement",
  "Santé & Sécurité",
  "Finance",
  "Autre",
];

const CATEGORY_COLORS: Record<string, string> = {
  "Pharmaceutique": "bg-purple-50 text-purple-700 border-purple-200",
  "Qualité": "bg-blue-50 text-blue-700 border-blue-200",
  "Cybersécurité": "bg-slate-50 text-slate-700 border-slate-300",
  "Alimentaire": "bg-green-50 text-green-700 border-green-200",
  "Environnement": "bg-emerald-50 text-emerald-700 border-emerald-200",
  "Santé & Sécurité": "bg-orange-50 text-orange-700 border-orange-200",
  "Finance": "bg-amber-50 text-amber-700 border-amber-200",
  "Autre": "bg-slate-50 text-slate-600 border-slate-200",
};

// Fallback static data when API is unavailable (marketing page needs to work unauthenticated)
const FALLBACK: PublicReferential[] = [
  { id: "1", name: "ISO 9001:2015", description: "Système de management de la qualité", category: "Qualité", sectionCount: 10, questionCount: 94, isSystem: true },
  { id: "2", name: "ISO 14001:2015", description: "Système de management environnemental", category: "Environnement", sectionCount: 10, questionCount: 72, isSystem: true },
  { id: "3", name: "ISO 45001:2018", description: "Système de management de la santé et sécurité au travail", category: "Santé & Sécurité", sectionCount: 10, questionCount: 68, isSystem: true },
  { id: "4", name: "ISO 27001:2022", description: "Sécurité des systèmes d'information", category: "Cybersécurité", sectionCount: 14, questionCount: 113, isSystem: true },
  { id: "5", name: "ISO 22000:2018", description: "Systèmes de management de la sécurité des denrées alimentaires", category: "Alimentaire", sectionCount: 8, questionCount: 85, isSystem: true },
  { id: "6", name: "GMP / EU-GMP Annex 11", description: "Bonnes pratiques de fabrication — validation des systèmes informatisés", category: "Pharmaceutique", sectionCount: 6, questionCount: 48, isSystem: true },
  { id: "7", name: "FDA 21 CFR Part 11", description: "Dossiers électroniques et signatures électroniques", category: "Pharmaceutique", sectionCount: 5, questionCount: 41, isSystem: true },
  { id: "8", name: "NF EN 9100:2018", description: "Système de management de la qualité — aéronautique, espace et défense", category: "Qualité", sectionCount: 10, questionCount: 102, isSystem: true },
  { id: "9", name: "SOC 2 Type II", description: "Contrôles de sécurité et disponibilité pour services SaaS", category: "Cybersécurité", sectionCount: 5, questionCount: 64, isSystem: true },
  { id: "10", name: "IATF 16949:2016", description: "Système de management de la qualité — industrie automobile", category: "Qualité", sectionCount: 10, questionCount: 118, isSystem: true },
  { id: "11", name: "ISO 50001:2018", description: "Système de management de l'énergie", category: "Environnement", sectionCount: 9, questionCount: 57, isSystem: true },
  { id: "12", name: "PCI DSS v4.0", description: "Sécurité des données de l'industrie des cartes de paiement", category: "Finance", sectionCount: 12, questionCount: 96, isSystem: true },
  { id: "13", name: "ISO 13485:2016", description: "Dispositifs médicaux — management de la qualité", category: "Pharmaceutique", sectionCount: 8, questionCount: 78, isSystem: true },
  { id: "14", name: "HACCP Codex Alimentarius", description: "Analyse des dangers et points critiques pour leur maîtrise", category: "Alimentaire", sectionCount: 7, questionCount: 52, isSystem: true },
  { id: "15", name: "NIS2 / DORA", description: "Résilience opérationnelle numérique — secteur financier UE", category: "Cybersécurité", sectionCount: 10, questionCount: 88, isSystem: true },
  { id: "16", name: "ISO 31000:2018", description: "Management du risque — lignes directrices", category: "Qualité", sectionCount: 6, questionCount: 44, isSystem: true },
  { id: "17", name: "BRC Global Standards v9", description: "Sécurité alimentaire mondiale — référentiel britannique", category: "Alimentaire", sectionCount: 9, questionCount: 91, isSystem: true },
  { id: "18", name: "OHSAS 18001 (transition ISO 45001)", description: "Santé et sécurité au travail — transition standard", category: "Santé & Sécurité", sectionCount: 8, questionCount: 61, isSystem: true },
];

export default function MarketplacePage() {
  const [referentials, setReferentials] = useState<PublicReferential[]>([]);
  const [search, setSearch] = useState("");
  const [activeCategory, setActiveCategory] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5050";
    fetch(`${apiUrl}/api/referentials/public`)
      .then(r => r.ok ? r.json() : Promise.reject())
      .then((data: PublicReferential[]) => setReferentials(data))
      .catch(() => setReferentials(FALLBACK))
      .finally(() => setLoading(false));
  }, []);

  const categories = Array.from(new Set(referentials.map(r => r.category ?? "Autre")))
    .sort((a, b) => {
      const ia = CATEGORY_ORDER.indexOf(a);
      const ib = CATEGORY_ORDER.indexOf(b);
      return (ia === -1 ? 99 : ia) - (ib === -1 ? 99 : ib);
    });

  const filtered = referentials.filter(r => {
    const matchSearch = !search || r.name.toLowerCase().includes(search.toLowerCase()) ||
      r.description?.toLowerCase().includes(search.toLowerCase());
    const matchCat = !activeCategory || (r.category ?? "Autre") === activeCategory;
    return matchSearch && matchCat;
  });

  const grouped = categories.reduce<Record<string, PublicReferential[]>>((acc, cat) => {
    const items = filtered.filter(r => (r.category ?? "Autre") === cat);
    if (items.length > 0) acc[cat] = items;
    return acc;
  }, {});

  const totalQuestions = referentials.reduce((s, r) => s + r.questionCount, 0);

  return (
    <main>
      {/* Hero */}
      <section className="text-center py-20 px-6 bg-gradient-to-b from-slate-50 to-white">
        <p className="text-sm font-semibold text-blue-600 mb-3 uppercase tracking-wide">Bibliothèque de référentiels</p>
        <h1 className="text-4xl md:text-5xl font-bold text-slate-900 mb-4">
          {referentials.length} référentiels prêts à l&apos;emploi
        </h1>
        <p className="text-lg text-slate-500 max-w-xl mx-auto mb-8">
          {totalQuestions.toLocaleString("fr-FR")} questions de contrôle couvrant ISO, GMP, HACCP, cybersécurité et plus encore.
          Importez en un clic dans votre compte.
        </p>
        <div className="flex items-center justify-center gap-6 text-sm text-slate-500 mb-10">
          <span className="flex items-center gap-1.5"><CheckCircle2 className="h-4 w-4 text-emerald-500" /> Mis à jour régulièrement</span>
          <span className="flex items-center gap-1.5"><CheckCircle2 className="h-4 w-4 text-emerald-500" /> Personnalisables</span>
          <span className="flex items-center gap-1.5"><CheckCircle2 className="h-4 w-4 text-emerald-500" /> Multilingues</span>
        </div>

        {/* Search */}
        <div className="max-w-lg mx-auto relative">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <input
            type="text"
            placeholder="Rechercher un référentiel (ISO 9001, HACCP, GMP…)"
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="w-full pl-10 pr-4 py-3 rounded-xl border border-slate-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white shadow-sm"
          />
        </div>
      </section>

      {/* Category filter */}
      <section className="max-w-6xl mx-auto px-6 pb-4">
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => setActiveCategory(null)}
            className={`px-4 py-1.5 rounded-full text-sm font-medium border transition-colors ${
              activeCategory === null
                ? "bg-blue-600 text-white border-blue-600"
                : "bg-white text-slate-600 border-slate-200 hover:border-slate-300"
            }`}
          >
            Tous ({referentials.length})
          </button>
          {categories.map(cat => (
            <button
              key={cat}
              onClick={() => setActiveCategory(activeCategory === cat ? null : cat)}
              className={`px-4 py-1.5 rounded-full text-sm font-medium border transition-colors ${
                activeCategory === cat
                  ? "bg-blue-600 text-white border-blue-600"
                  : "bg-white text-slate-600 border-slate-200 hover:border-slate-300"
              }`}
            >
              {cat} ({referentials.filter(r => (r.category ?? "Autre") === cat).length})
            </button>
          ))}
        </div>
      </section>

      {/* Grid */}
      <section className="max-w-6xl mx-auto px-6 pb-20">
        {loading ? (
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4 mt-6">
            {[...Array(6)].map((_, i) => (
              <div key={i} className="h-44 bg-slate-100 rounded-2xl animate-pulse" />
            ))}
          </div>
        ) : Object.keys(grouped).length === 0 ? (
          <p className="text-center text-slate-400 py-20">Aucun référentiel trouvé pour &quot;{search}&quot;</p>
        ) : (
          Object.entries(grouped).map(([cat, items]) => (
            <div key={cat} className="mt-10">
              <div className="flex items-center gap-3 mb-4">
                <span className={`flex items-center gap-1.5 text-xs font-semibold px-3 py-1 rounded-full border ${CATEGORY_COLORS[cat] ?? CATEGORY_COLORS["Autre"]}`}>
                  <Tag className="h-3 w-3" />
                  {cat}
                </span>
                <span className="text-sm text-slate-400">{items.length} référentiel{items.length > 1 ? "s" : ""}</span>
              </div>

              <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
                {items.map(ref => (
                  <div key={ref.id} className="bg-white border border-slate-200 rounded-2xl p-5 flex flex-col hover:shadow-md transition-shadow">
                    <div className="flex items-start justify-between gap-2 mb-2">
                      <div className="p-2 bg-blue-50 rounded-lg shrink-0">
                        <BookOpen className="h-4 w-4 text-blue-600" />
                      </div>
                      {ref.isSystem && (
                        <span className="text-[10px] font-bold px-2 py-0.5 bg-slate-100 text-slate-500 rounded-full">
                          Système
                        </span>
                      )}
                    </div>
                    <h3 className="font-semibold text-slate-800 text-sm mb-1 leading-snug">{ref.name}</h3>
                    {ref.description && (
                      <p className="text-xs text-slate-500 mb-3 flex-1 leading-relaxed line-clamp-2">{ref.description}</p>
                    )}
                    <div className="flex items-center justify-between mt-auto pt-3 border-t border-slate-100">
                      <div className="flex gap-3 text-xs text-slate-400">
                        <span>{ref.sectionCount} sections</span>
                        <span>{ref.questionCount} questions</span>
                      </div>
                      <Link
                        href="/sign-up"
                        className="flex items-center gap-1 text-xs font-semibold text-blue-600 hover:text-blue-700 transition-colors"
                      >
                        Utiliser <ArrowRight className="h-3 w-3" />
                      </Link>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))
        )}
      </section>

      {/* CTA */}
      <section className="bg-blue-600 py-16 px-6 text-center text-white">
        <h2 className="text-3xl font-bold mb-3">Votre référentiel n&apos;est pas dans la liste ?</h2>
        <p className="text-blue-100 mb-8 max-w-lg mx-auto">
          Importez votre propre référentiel en quelques minutes ou demandez-nous de l&apos;ajouter à la bibliothèque.
        </p>
        <div className="flex flex-col sm:flex-row gap-3 justify-center">
          <Link
            href="/sign-up"
            className="inline-flex items-center gap-2 bg-white text-blue-600 font-bold px-8 py-3 rounded-xl hover:bg-blue-50 transition-colors"
          >
            Créer mon référentiel <ArrowRight className="h-5 w-5" />
          </Link>
          <a
            href="mailto:hello@auditbridge.io"
            className="inline-flex items-center gap-2 bg-blue-500 text-white font-semibold px-8 py-3 rounded-xl hover:bg-blue-400 transition-colors"
          >
            Proposer un référentiel
          </a>
        </div>
      </section>
    </main>
  );
}
