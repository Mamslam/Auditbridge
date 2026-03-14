import Link from "next/link";
import { Shield } from "lucide-react";

export default function MarketingLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-white">
      <header className="border-b border-slate-100 sticky top-0 bg-white/90 backdrop-blur z-10">
        <div className="max-w-6xl mx-auto px-6 h-16 flex items-center justify-between">
          <Link href="/" className="flex items-center gap-2 font-bold text-slate-900">
            <Shield className="h-5 w-5 text-blue-600" />
            AuditBridge
          </Link>
          <nav className="hidden md:flex items-center gap-6 text-sm text-slate-600">
            <Link href="/marketplace" className="hover:text-slate-900 transition-colors">Référentiels</Link>
            <Link href="/pricing" className="hover:text-slate-900 transition-colors">Tarifs</Link>
          </nav>
          <div className="flex items-center gap-3">
            <Link href="/sign-in" className="text-sm text-slate-600 hover:text-slate-900 transition-colors">
              Connexion
            </Link>
            <Link
              href="/sign-up"
              className="bg-blue-600 text-white text-sm font-semibold px-4 py-2 rounded-xl hover:bg-blue-700 transition-colors"
            >
              Essai gratuit
            </Link>
          </div>
        </div>
      </header>

      {children}

      <footer className="border-t border-slate-100 py-10 mt-20">
        <div className="max-w-6xl mx-auto px-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2 text-slate-500 text-sm">
            <Shield className="h-4 w-4 text-blue-500" />
            <span>AuditBridge — La plateforme d&apos;audit qui connecte l&apos;Europe</span>
          </div>
          <div className="flex gap-6 text-sm text-slate-400">
            <Link href="/pricing" className="hover:text-slate-600">Tarifs</Link>
            <Link href="/marketplace" className="hover:text-slate-600">Référentiels</Link>
            <a href="mailto:hello@auditbridge.io" className="hover:text-slate-600">Contact</a>
          </div>
        </div>
      </footer>
    </div>
  );
}
