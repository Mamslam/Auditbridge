import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "AuditBridge",
    short_name: "AuditBridge",
    description: "La plateforme d'audit qui connecte l'Europe",
    start_url: "/auditor/dashboard",
    display: "standalone",
    background_color: "#ffffff",
    theme_color: "#2563eb",
    orientation: "portrait-primary",
    icons: [
      {
        src: "/icon-192.png",
        sizes: "192x192",
        type: "image/png",
        purpose: "maskable",
      },
      {
        src: "/icon-512.png",
        sizes: "512x512",
        type: "image/png",
      },
    ],
    categories: ["business", "productivity"],
    lang: "fr",
  };
}
