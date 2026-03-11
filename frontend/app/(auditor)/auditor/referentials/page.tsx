"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Search, Plus, Copy, BookOpen, ChevronRight, Lock } from "lucide-react";
import { referentialsApi } from "@/lib/api/referentials";
import type { Referential, ReferentialCategory } from "@/lib/types";
import { cn } from "@/lib/utils";

export default function ReferentialsPage() {
  const [referentials, setReferentials] = useState<Referential[]>([]);
  const [categories, setCategories] = useState<ReferentialCategory[]>([]);
  const [search, setSearch] = useState("");
  const [activeCategory, setActiveCategory] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [duplicating, setDuplicating] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      referentialsApi.getAll(),
      referentialsApi.getCategories(),
    ]).then(([refs, cats]) => {
      setReferentials(refs);
      setCategories(cats);
    }).finally(() => setLoading(false));
  }, []);

  const filtered = referentials.filter((r) => {
    const matchesSearch =
      !search ||
      r.name.toLowerCase().includes(search.toLowerCase()) ||
      r.slug.toLowerCase().includes(search.toLowerCase());
    const matchesCategory = !activeCategory || r.category?.slug === activeCategory;
    return matchesSearch && matchesCategory;
  });

  const handleDuplicate = async (id: string) => {
    setDuplicating(id);
    try {
      const copy = await referentialsApi.duplicate(id);
      setReferentials((prev) => [...prev, copy]);
    } finally {
      setDuplicating(null);
    }
  };

  return (
    <div className="p-8 max-w-6xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Référentiels</h1>
          <p className="text-slate-500 text-sm mt-0.5">
            {referentials.filter((r) => r.isSystem).length} référentiels système ·{" "}
            {referentials.filter((r) => !r.isSystem).length} personnalisés
          </p>
        </div>
        <Link
          href="/auditor/referentials/new"
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Nouveau référentiel
        </Link>
      </div>

      {/* Search + filter */}
      <div className="flex gap-3 mb-6">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Rechercher..."
            className="w-full pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
          />
        </div>
        <div className="flex gap-2 flex-wrap">
          <button
            onClick={() => setActiveCategory(null)}
            className={cn(
              "px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors",
              !activeCategory
                ? "bg-blue-600 text-white border-blue-600"
                : "bg-white text-slate-600 border-slate-200 hover:border-slate-300"
            )}
          >
            Tous
          </button>
          {categories.map((cat) => (
            <button
              key={cat.slug}
              onClick={() => setActiveCategory(activeCategory === cat.slug ? null : cat.slug)}
              className={cn(
                "px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors",
                activeCategory === cat.slug
                  ? "bg-blue-600 text-white border-blue-600"
                  : "bg-white text-slate-600 border-slate-200 hover:border-slate-300"
              )}
            >
              {cat.icon} {cat.label}
            </button>
          ))}
        </div>
      </div>

      {/* Grid */}
      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[...Array(9)].map((_, i) => (
            <div key={i} className="bg-white rounded-2xl border border-slate-200 p-5 animate-pulse">
              <div className="h-4 bg-slate-100 rounded w-2/3 mb-3" />
              <div className="h-3 bg-slate-100 rounded w-1/3 mb-4" />
              <div className="h-3 bg-slate-100 rounded w-full" />
            </div>
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-16">
          <BookOpen className="h-10 w-10 text-slate-300 mx-auto mb-3" />
          <p className="text-slate-500 text-sm">Aucun référentiel trouvé</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map((ref) => (
            <ReferentialCard
              key={ref.id}
              ref={ref}
              onDuplicate={handleDuplicate}
              duplicating={duplicating === ref.id}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function ReferentialCard({
  ref: referential,
  onDuplicate,
  duplicating,
}: {
  ref: Referential;
  onDuplicate: (id: string) => void;
  duplicating: boolean;
}) {
  return (
    <div className="bg-white rounded-2xl border border-slate-200 p-5 hover:shadow-sm hover:border-slate-300 transition-all flex flex-col">
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-2.5">
          <div
            className="h-8 w-8 rounded-lg flex items-center justify-center text-base"
            style={{ backgroundColor: referential.category?.color + "20" }}
          >
            {referential.category?.icon ?? "📋"}
          </div>
          <div>
            <h3 className="font-semibold text-slate-900 text-sm leading-tight">{referential.name}</h3>
            <p className="text-xs text-slate-400">{referential.version}</p>
          </div>
        </div>
        {referential.isSystem && (
          <span className="flex items-center gap-1 text-[10px] font-medium text-slate-500 bg-slate-100 px-2 py-0.5 rounded-full">
            <Lock className="h-2.5 w-2.5" />
            Système
          </span>
        )}
      </div>

      {referential.description && (
        <p className="text-xs text-slate-500 line-clamp-2 mb-3">{referential.description}</p>
      )}

      <div className="flex items-center gap-2 mt-auto pt-3 border-t border-slate-100">
        <button
          onClick={() => onDuplicate(referential.id)}
          disabled={duplicating}
          className="flex items-center gap-1.5 text-xs text-slate-500 hover:text-blue-600 transition-colors disabled:opacity-50"
        >
          <Copy className="h-3.5 w-3.5" />
          {duplicating ? "Copie..." : "Dupliquer"}
        </button>
        {!referential.isSystem && (
          <Link
            href={`/auditor/templates?ref=${referential.id}`}
            className="ml-auto flex items-center gap-1.5 text-xs text-blue-600 hover:text-blue-700 font-medium transition-colors"
          >
            Éditer <ChevronRight className="h-3 w-3" />
          </Link>
        )}
      </div>
    </div>
  );
}
