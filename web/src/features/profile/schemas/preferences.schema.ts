import { z } from "zod";
import { locales } from "@/i18n/routing";

export const themes = ["light", "dark", "system"] as const;

export const preferencesSchema = z.object({
  locale: z.enum(locales),
  theme: z.enum(themes),
});

export type PreferencesFormValues = z.infer<typeof preferencesSchema>;
export type ThemePreference = (typeof themes)[number];
