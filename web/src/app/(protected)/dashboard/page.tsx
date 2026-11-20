import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { QuickLinks } from "@/features/dashboard/components/quick-links";
import { RecentLogins } from "@/features/dashboard/components/recent-logins";
import { SummaryCards } from "@/features/dashboard/components/summary-cards";
import { getUsersSummary } from "@/features/dashboard/services/dashboard.service";
import { getSession } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("pages.dashboard");
  return { title: t("metadata") };
}

export default async function DashboardPage() {
  const t = await getTranslations("pages.dashboard");
  const session = await getSession("/dashboard");
  if (!session.modules.some((module) => module.key === "dashboard")) {
    redirect("/403");
  }
  const hasUserRead = session.permissions.includes("users.read");
  const { summary, error: summaryError } = hasUserRead
    ? await getUsersSummary()
    : { summary: null, error: false };

  return (
    <div className="space-y-10">
      <section>
        <p className="text-sm font-medium text-primary">{t("summary")}</p>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">
          {t("greeting", { name: session.fullName.split(" ")[0] || session.fullName })}
        </h1>
        <p className="mt-2 text-muted-foreground">
          {hasUserRead
            ? t("organizationOverview")
            : t("workspace")}
        </p>
      </section>

      {hasUserRead && summary && (
        <section className="space-y-4">
          <SummaryCards summary={summary} />
          <RecentLogins users={summary.recentLogins} />
        </section>
      )}
      {summaryError && (
        <p role="alert" className="rounded-xl border border-destructive/20 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {t("summaryError")}
        </p>
      )}
      <QuickLinks modules={session.modules} />
    </div>
  );
}
