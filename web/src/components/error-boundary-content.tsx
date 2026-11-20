"use client";

import { useTranslations } from "next-intl";
import { correlationIdFromError } from "@/lib/correlation";

export function ErrorBoundaryContent({
  error,
  reset,
}: Readonly<{ error: Error & { digest?: string }; reset: () => void }>) {
  const t = useTranslations("errorBoundary");
  const correlationId = correlationIdFromError(error);

  return (
    <main className="mx-auto flex min-h-screen max-w-lg items-center p-6">
      <section className="w-full rounded-2xl border border-destructive/20 bg-card p-8 text-center shadow-sm" role="alert">
        <h1 className="text-2xl font-semibold">{t("title")}</h1>
        <p className="mt-3 text-sm text-muted-foreground">{t("description")}</p>
        {correlationId ? (
          <p className="mt-4 break-all text-xs text-muted-foreground">
            {t("correlationId", { correlationId })}
          </p>
        ) : null}
        <button
          type="button"
          className="mt-6 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
          onClick={reset}
        >
          {t("retry")}
        </button>
      </section>
    </main>
  );
}
