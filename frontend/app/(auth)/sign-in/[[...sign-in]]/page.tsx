import { redirect } from "next/navigation";

const hasRealClerkKey =
  process.env.NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY?.startsWith("pk_") &&
  !process.env.NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY?.includes("YOUR_CLERK");

export default async function SignInPage() {
  if (!hasRealClerkKey) {
    redirect("/auditor/dashboard");
  }

  const { SignIn } = await import("@clerk/nextjs");
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 flex items-center justify-center p-4">
      <SignIn
        appearance={{
          elements: {
            rootBox: "mx-auto",
            card: "shadow-sm border border-slate-200",
          },
        }}
        fallbackRedirectUrl="/auditor/dashboard"
        signUpUrl="/sign-up"
      />
    </div>
  );
}
