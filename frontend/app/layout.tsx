import type { Metadata } from "next";
import { Geist } from "next/font/google";
import { ClerkProvider } from "@clerk/nextjs";
import { Toaster } from "@/components/ui/sonner";
import "./globals.css";

const hasRealClerkKey =
  process.env.NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY?.startsWith("pk_") &&
  !process.env.NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY?.includes("YOUR_CLERK");

function ConditionalClerkProvider({ children }: { children: React.ReactNode }) {
  if (!hasRealClerkKey) return <>{children}</>;
  return <ClerkProvider>{children}</ClerkProvider>;
}

const geist = Geist({
  variable: "--font-geist",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "AuditBridge — La plateforme d'audit qui connecte l'Europe",
  description:
    "Gérez vos audits GMP, ISO, NIS2, RGPD et plus encore avec l'intelligence artificielle.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <ConditionalClerkProvider>
      <html lang="fr">
        <body className={`${geist.variable} font-sans antialiased`}>
          {children}
          <Toaster richColors position="top-right" />
        </body>
      </html>
    </ConditionalClerkProvider>
  );
}
