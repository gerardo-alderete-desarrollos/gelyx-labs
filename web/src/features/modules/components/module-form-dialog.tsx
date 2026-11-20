"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { iconMap } from "@/lib/icon-map";
import { Button } from "@/components/ui/button";
import {
  moduleFormSchema,
  type ManagedModule,
  type ModuleFormInput,
  type ModuleFormValues,
} from "@/features/modules/schemas/modules.schema";
import { ModulesRequestError } from "@/features/modules/services/modules.service";
import { locales } from "@/i18n/routing";
import { useTranslations } from "next-intl";

function valuesFor(module: ManagedModule): ModuleFormInput {
  return {
    icon: module.icon ?? "",
    sortOrder: module.sortOrder,
    translations: locales.map((locale) => {
      const translation = module.translations.find((item) => item.locale === locale);
      return { locale, name: translation?.name ?? "", description: translation?.description ?? "" };
    }),
  };
}

export function ModuleFormDialog({
  module,
  onClose,
  onSubmit,
}: Readonly<{
  module: ManagedModule;
  onClose: () => void;
  onSubmit: (values: ModuleFormValues) => Promise<void>;
}>) {
  const t = useTranslations("modules");
  const common = useTranslations("common");
  const errors = useTranslations("errors");
  const validation = useTranslations("auth.validation");
  const form = useForm<ModuleFormInput, undefined, ModuleFormValues>({
    resolver: zodResolver(moduleFormSchema),
    defaultValues: valuesFor(module),
  });
  const submit = async (values: ModuleFormValues) => {
    try {
      await onSubmit(values);
    } catch (error) {
      if (error instanceof ModulesRequestError && error.status === 422 && Object.keys(error.fieldErrors).length) {
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          form.setError(field as keyof ModuleFormInput, {
            message: errors.has(error.code) ? errors(error.code) : messages.join(" "),
          });
        }
        return;
      }
      const code = error instanceof ModulesRequestError ? error.code : "generic";
      form.setError("root.server", { message: errors.has(code) ? errors(code) : errors("generic") });
    }
  };

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <section role="dialog" aria-modal="true" aria-labelledby="module-form-title" className="max-h-[calc(100vh-2rem)] w-full max-w-2xl overflow-y-auto rounded-2xl border border-border bg-card p-6 shadow-xl">
        <h2 id="module-form-title" className="text-xl font-semibold">{t("editTitle", { key: module.key })}</h2>
        <form className="mt-5 space-y-5" onSubmit={form.handleSubmit(submit)}>
          {locales.map((locale, index) => (
            <fieldset key={locale} className="space-y-3 rounded-xl border border-border p-4">
              <legend className="px-1 text-sm font-medium">{t(`locale.${locale}`)}</legend>
              <input type="hidden" {...form.register(`translations.${index}.locale`)} />
              <label className="block space-y-1.5 text-sm">
                <span>{t("name", { locale: t(`locale.${locale}`) })}</span>
                <input aria-label={t("name", { locale: t(`locale.${locale}`) })} className="h-10 w-full rounded-lg border border-input bg-background px-3" {...form.register(`translations.${index}.name`)} />
                {form.formState.errors.translations?.[index]?.name && <span role="alert" className="text-destructive">{validation(form.formState.errors.translations[index]?.name?.message ?? "requiredModuleName")}</span>}
              </label>
              <label className="block space-y-1.5 text-sm">
                <span>{t("description", { locale: t(`locale.${locale}`) })}</span>
                <textarea aria-label={t("description", { locale: t(`locale.${locale}`) })} rows={3} className="w-full rounded-lg border border-input bg-background px-3 py-2" {...form.register(`translations.${index}.description`)} />
                {form.formState.errors.translations?.[index]?.description && <span role="alert" className="text-destructive">{validation(form.formState.errors.translations[index]?.description?.message ?? "moduleDescriptionTooLong")}</span>}
              </label>
            </fieldset>
          ))}
          <label className="block space-y-1.5 text-sm">
            <span>{t("icon")}</span>
            <select aria-label={t("icon")} className="h-10 w-full rounded-lg border border-input bg-background px-3" {...form.register("icon")}>
              <option value="">{t("selectIcon")}</option>
              {Object.keys(iconMap).sort().map((icon) => <option key={icon} value={icon}>{icon}</option>)}
            </select>
            {form.formState.errors.icon && <span role="alert" className="text-destructive">{validation(form.formState.errors.icon.message ?? "requiredIcon")}</span>}
          </label>
          <label className="block space-y-1.5 text-sm">
            <span>{t("sortOrder")}</span>
            <input aria-label={t("sortOrder")} type="number" min="0" step="1" className="h-10 w-full rounded-lg border border-input bg-background px-3" {...form.register("sortOrder")} />
            {form.formState.errors.sortOrder && <span role="alert" className="text-destructive">{validation(form.formState.errors.sortOrder.message ?? "invalidSortOrder")}</span>}
          </label>
          {form.formState.errors.root?.server?.message && <p role="alert" className="text-sm text-destructive">{form.formState.errors.root.server.message}</p>}
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={onClose}>{common("cancel")}</Button>
            <Button type="submit" disabled={form.formState.isSubmitting}>{form.formState.isSubmitting ? common("saving") : common("save")}</Button>
          </div>
        </form>
      </section>
    </div>
  );
}
