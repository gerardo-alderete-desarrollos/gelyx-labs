"use client";

import { useTranslations } from "next-intl";

export default function DashboardError({ reset }: Readonly<{ reset: () => void }>) {
  const t = useTranslations("pages.dashboard");
  return (
    <div className="rounded-2xl border border-destructive/20 bg-card p-8 text-center">
      <h1 className="text-xl font-semibold">{t("loadError")}</h1>
      <p className="mt-2 text-sm text-muted-foreground">{t("loadErrorDescription")}</p>
      <button type="button" onClick={reset} className="mt-5 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground">
        {t("retry")}
      </button>
    </div>
  );
}
