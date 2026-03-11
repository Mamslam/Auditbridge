"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  ClipboardCheck,
  LayoutDashboard,
  FolderOpen,
  BookOpen,
  FileText,
  Users,
  LogOut,
  ChevronRight,
} from "lucide-react";
import { cn } from "@/lib/utils";

const NAV = [
  { href: "/auditor/dashboard", icon: LayoutDashboard, label: "Dashboard" },
  { href: "/auditor/audits", icon: ClipboardCheck, label: "Audits" },
  { href: "/auditor/referentials", icon: BookOpen, label: "Référentiels" },
  { href: "/auditor/templates", icon: FolderOpen, label: "Éditeur" },
  { href: "/auditor/reports", icon: FileText, label: "Rapports" },
  { href: "/auditor/clients", icon: Users, label: "Clients" },
];

export default function AuditorLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  return (
    <div className="flex h-screen bg-slate-50">
      {/* Sidebar */}
      <aside className="w-60 shrink-0 bg-white border-r border-slate-200 flex flex-col">
        {/* Logo */}
        <div className="h-16 flex items-center gap-2.5 px-5 border-b border-slate-100">
          <div className="h-7 w-7 rounded-lg bg-blue-600 flex items-center justify-center">
            <ClipboardCheck className="h-3.5 w-3.5 text-white" />
          </div>
          <span className="font-bold text-slate-900">AuditBridge</span>
          <span className="ml-auto text-[10px] font-semibold text-blue-600 bg-blue-50 px-1.5 py-0.5 rounded">
            Auditeur
          </span>
        </div>

        {/* Nav */}
        <nav className="flex-1 px-3 py-4 space-y-0.5">
          {NAV.map(({ href, icon: Icon, label }) => {
            const active = pathname === href || pathname.startsWith(href + "/");
            return (
              <Link
                key={href}
                href={href}
                className={cn(
                  "flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors",
                  active
                    ? "bg-blue-50 text-blue-700"
                    : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                )}
              >
                <Icon className="h-4 w-4 shrink-0" />
                {label}
                {active && <ChevronRight className="h-3.5 w-3.5 ml-auto" />}
              </Link>
            );
          })}
        </nav>

        {/* Footer */}
        <div className="p-3 border-t border-slate-100">
          <Link
            href="/sign-out"
            className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium text-slate-500 hover:text-red-600 hover:bg-red-50 transition-colors"
          >
            <LogOut className="h-4 w-4" />
            Déconnexion
          </Link>
        </div>
      </aside>

      {/* Main */}
      <main className="flex-1 overflow-y-auto">
        {children}
      </main>
    </div>
  );
}
