import { redirect } from "next/navigation";
import { AppHeader } from "@/components/layout/app-header";
import { SessionProvider } from "@/features/auth/components/session-provider";
import { getSession } from "@/lib/auth/get-session";

export default async function DashboardLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const session = await getSession("/dashboard");
  if (session.mustChangePassword) {
    redirect("/change-password");
  }

  return (
    <SessionProvider initialSession={session}>
      <div className="min-h-screen">
        <AppHeader session={session} />
        <div className="md:ml-64 min-h-[calc(100vh-76px)]">
          <main className="mx-auto max-w-7xl px-5 py-8 md:px-8 md:py-10">{children}</main>
        </div>
      </div>
    </SessionProvider>
  );
}
