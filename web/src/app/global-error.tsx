"use client";

import "./globals.css";
import { NextIntlClientProvider } from "next-intl";
import english from "../../messages/en.json";
import spanish from "../../messages/es.json";
import { ErrorBoundaryContent } from "@/components/error-boundary-content";

export default function GlobalError({
  error,
  reset,
}: Readonly<{ error: Error & { digest?: string }; reset: () => void }>) {
  const locale = typeof document !== "undefined" && document.documentElement.lang === "en" ? "en" : "es";
  const messages = locale === "en" ? english : spanish;

  return (
    <html lang={locale}>
      <body className="min-h-screen bg-background font-sans text-foreground antialiased">
        <NextIntlClientProvider locale={locale} messages={messages}>
          <ErrorBoundaryContent error={error} reset={reset} />
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
