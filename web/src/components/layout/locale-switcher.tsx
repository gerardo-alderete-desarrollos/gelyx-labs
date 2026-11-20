"use client";

import { useTransition } from "react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { fetchBff } from "@/lib/api/browser-client";
import { isAppLocale, locales } from "@/i18n/routing";
import { notify } from "@/lib/notify";

export function LocaleSwitcher({ authenticated = false }: Readonly<{ authenticated?: boolean }>) {
  const selectedLocale = useLocale();
  const locale = isAppLocale(selectedLocale) ? selectedLocale : "es";
  const router = useRouter();
  const t = useTranslations("common");
  const errors = useTranslations("errors");
  const [pending, startTransition] = useTransition();
  const localeNames = new Intl.DisplayNames([locale], { type: "language" });

  async function changeLocale(value: string) {
    if (!isAppLocale(value)) return;
    const nextLocale = value;
    if (nextLocale === locale) return;
    if (authenticated) {
      try {
        const response = await fetchBff("/api/auth/me/preferences", {
          method: "PUT",
          body: JSON.stringify({ locale: nextLocale }),
        });
        if (!response.ok) {
          const body = (await response.json().catch(() => null)) as { code?: string } | null;
          const code = body?.code;
          notify.error(code && errors.has(code) ? errors(code) : errors("generic"));
          return;
        }
      } catch {
        notify.error(errors("generic"));
        return;
      }
    }

    const secure = window.location.protocol === "https:" ? "; Secure" : "";
    document.cookie = `NEXT_LOCALE=${nextLocale}; Path=/; Max-Age=31536000; SameSite=Lax${secure}`;
    startTransition(() => router.refresh());
  }

  return (
    <label className="inline-flex items-center gap-2 text-sm">
      <span className="sr-only">{t("language")}</span>
      <select
        aria-label={t("language")}
        className="h-9 rounded-lg border border-input bg-background px-2"
        value={locale}
        disabled={pending}
        onChange={(event) => void changeLocale(event.target.value)}
      >
        {locales.map((availableLocale) => (
          <option key={availableLocale} value={availableLocale}>
            {localeNames.of(availableLocale) ?? availableLocale}
          </option>
        ))}
      </select>
    </label>
  );
}
