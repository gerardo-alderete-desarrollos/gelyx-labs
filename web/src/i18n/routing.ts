import { defineRouting } from "next-intl/routing";

export const locales = ["es", "en"] as const;
export type AppLocale = (typeof locales)[number];

export function isAppLocale(value: string): value is AppLocale {
  return locales.some((locale) => locale === value);
}

export const routing = defineRouting({
  locales,
  defaultLocale: "es",
  localePrefix: "never",
  localeCookie: {
    name: "NEXT_LOCALE",
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
  },
});
