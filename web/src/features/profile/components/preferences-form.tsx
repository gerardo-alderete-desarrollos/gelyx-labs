"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTheme } from "next-themes";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FieldLabel } from "@/components/ui/field-label";
import { useSession, sessionQueryKey } from "@/features/auth/hooks/use-session";
import { preferencesSchema, type PreferencesFormValues, type ThemePreference } from "@/features/profile/schemas/preferences.schema";
import { updatePreferences } from "@/features/profile/services/preferences.service";
import { isAppLocale } from "@/i18n/routing";
import { notify } from "@/lib/notify";
import { useLocale, useTranslations } from "next-intl";

function setLocaleCookie(locale: string) {
  const secure = window.location.protocol === "https:" ? "; Secure" : "";
  document.cookie = `NEXT_LOCALE=${locale}; Path=/; Max-Age=31536000; SameSite=Lax${secure}`;
}

export function PreferencesForm() {
  const t = useTranslations("profile");
  const apiErrors = useTranslations("errors");
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { data: session } = useSession();
  const { setTheme } = useTheme();
  const savedLocale = (session as { locale?: unknown } | undefined)?.locale;
  const currentLocale = typeof savedLocale === "string" && isAppLocale(savedLocale) ? savedLocale : isAppLocale(locale) ? locale : "es";
  const savedTheme = (session as { theme?: unknown } | undefined)?.theme;
  const currentTheme: ThemePreference = savedTheme === "light" || savedTheme === "dark" || savedTheme === "system"
    ? savedTheme
    : "system";
  const form = useForm<PreferencesFormValues>({
    resolver: zodResolver(preferencesSchema),
    values: { locale: currentLocale, theme: currentTheme },
  });

  useEffect(() => {
    setTheme(currentTheme);
  }, [currentTheme, setTheme]);

  const mutation = useMutation({
    mutationFn: updatePreferences,
    onSuccess: async (_result, values) => {
      setTheme(values.theme);
      setLocaleCookie(values.locale);
      await queryClient.invalidateQueries({ queryKey: sessionQueryKey });
      notify.success(t("preferencesUpdated"));
      if (values.locale !== locale) router.refresh();
    },
    onError: (error) => {
      const code = error instanceof Error ? error.message : "generic";
      notify.error(apiErrors.has(code) ? apiErrors(code) : apiErrors("generic"));
    },
  });

  return (
    <form onSubmit={form.handleSubmit((values) => mutation.mutate(values))} className="space-y-5">
      <div className="space-y-2">
        <FieldLabel htmlFor="locale">{t("language")}</FieldLabel>
        <select id="locale" className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm" {...form.register("locale")}>
          <option value="es">{t("spanish")}</option>
          <option value="en">{t("english")}</option>
        </select>
      </div>
      <div className="space-y-2">
        <FieldLabel htmlFor="theme">{t("theme")}</FieldLabel>
        <select id="theme" className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm" {...form.register("theme")}>
          <option value="light">{t("themeLight")}</option>
          <option value="dark">{t("themeDark")}</option>
          <option value="system">{t("themeSystem")}</option>
        </select>
        <p className="text-xs text-muted-foreground">{t("themeHelp")}</p>
      </div>
      <Button type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? t("saving") : t("savePreferences")}
      </Button>
    </form>
  );
}
