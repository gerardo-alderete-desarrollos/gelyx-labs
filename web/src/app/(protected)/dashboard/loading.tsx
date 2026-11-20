import { getTranslations } from "next-intl/server";

export default async function DashboardLoading() {
  const t = await getTranslations("pages.dashboard");
  return (
    <div className="animate-pulse space-y-8" aria-label={t("loading")}>
      <div className="space-y-3">
        <div className="h-4 w-24 rounded bg-muted" />
        <div className="h-9 w-64 rounded bg-muted" />
        <div className="h-4 w-96 max-w-full rounded bg-muted" />
      </div>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 3 }, (_, index) => <div key={index} className="h-36 rounded-2xl bg-muted" />)}
      </div>
      <div className="h-52 rounded-2xl bg-muted" />
    </div>
  );
}
