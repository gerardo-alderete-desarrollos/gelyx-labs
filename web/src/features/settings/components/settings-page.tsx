"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useSettings, settingsQueryKey } from "@/features/settings/hooks/use-settings";
import { settingValues, settingsService } from "@/features/settings/services/settings.service";
import type { SettingValue } from "@/features/settings/types/settings.types";
import { notify } from "@/lib/notify";

export function SettingsPage() {
  const t = useTranslations("settings");
  const common = useTranslations("common");
  const queryClient = useQueryClient();
  const query = useSettings();
  const values = settingValues(query.data ?? []);
  const mutation = useMutation({
    mutationFn: ({ key, value }: { key: string; value: SettingValue }) => settingsService.update(key, value),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: settingsQueryKey });
      await queryClient.invalidateQueries({ queryKey: ["public-settings"] });
      notify.success(t("saved"));
    },
    onError: (error) => notify.error(error),
  });

  const save = (key: string, value: SettingValue) => void mutation.mutate({ key, value });
  if (query.isPending) return <div className="h-64 animate-pulse rounded-2xl bg-muted" aria-label={t("loading")} />;
  if (query.isError) return <section role="alert" className="rounded-2xl border border-destructive/30 bg-card p-8 text-center"><p className="text-destructive">{t("loadError")}</p><Button className="mt-4" variant="outline" onClick={() => void query.refetch()}>{common("retry")}</Button></section>;

  return <div className="space-y-6">
    <header><p className="text-sm font-medium text-primary">{common("administration")}</p><h1 className="mt-1 text-3xl font-semibold tracking-tight">{t("title")}</h1><p className="mt-2 text-muted-foreground">{t("description")}</p></header>
    <section className="space-y-4 rounded-2xl border bg-card p-6"><h2 className="text-lg font-semibold">{t("applicationSection")}</h2>
      <label className="block space-y-2"><span className="text-sm font-medium">{t("appName")}</span><div className="flex gap-2"><Input defaultValue={typeof values["app.name"] === "string" ? values["app.name"] : ""} /><Button type="button" disabled={mutation.isPending} onClick={(event) => { const input = event.currentTarget.previousElementSibling as HTMLInputElement; save("app.name", input.value); }}>{common("save")}</Button></div></label>
      <label className="block space-y-2"><span className="text-sm font-medium">{t("defaultLocale")}</span><select className="h-9 rounded-md border bg-background px-3 text-sm" value={typeof values["app.defaultLocale"] === "string" ? values["app.defaultLocale"] : "es"} disabled={mutation.isPending} onChange={(event) => save("app.defaultLocale", event.target.value)}><option value="es">{t("spanish")}</option><option value="en">{t("english")}</option></select></label>
    </section>
    <section className="space-y-4 rounded-2xl border bg-card p-6"><h2 className="text-lg font-semibold">{t("authenticationSection")}</h2><label className="flex items-center justify-between gap-4"><span><span className="block font-medium">{t("allowSelfRegistration")}</span><span className="text-sm text-muted-foreground">{t("allowSelfRegistrationHelp")}</span></span><input type="checkbox" checked={values["auth.allowSelfRegistration"] === true} disabled={mutation.isPending} onChange={(event) => save("auth.allowSelfRegistration", event.target.checked)} /></label></section>
    {Object.keys(values).filter((key) => key.startsWith("flags.")).length > 0 && <section className="space-y-4 rounded-2xl border bg-card p-6"><h2 className="text-lg font-semibold">{t("flagsSection")}</h2>{Object.keys(values).filter((key) => key.startsWith("flags.")).map((key) => <label key={key} className="flex items-center justify-between gap-4"><span className="font-medium">{key.slice("flags.".length)}</span><input type="checkbox" checked={values[key] === true} disabled={mutation.isPending} onChange={(event) => save(key, event.target.checked)} /></label>)}</section>}
  </div>;
}
