"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import {
  roleFormSchema,
  type Role,
  type RoleFormInput,
  type RoleFormValues,
} from "@/features/roles/schemas/roles.schema";
import { RolesRequestError } from "@/features/roles/services/roles.service";
import { useTranslations } from "next-intl";

export function RoleFormDialog({
  roles,
  role,
  onClose,
  onSubmit,
}: Readonly<{
  roles: Role[];
  role?: Role;
  onClose: () => void;
  onSubmit: (values: RoleFormValues) => Promise<void>;
}>) {
  const t = useTranslations("roles");
  const common = useTranslations("common");
  const errors = useTranslations("errors");
  const validation = useTranslations("auth.validation");
  const form = useForm<RoleFormInput, undefined, RoleFormValues>({
    resolver: zodResolver(roleFormSchema),
    defaultValues: {
      name: role?.name ?? "",
      description: role?.description ?? "",
      copyPermissionsFromRoleId: undefined,
    },
  });
  const submit = async (values: RoleFormValues) => {
    try {
      await onSubmit(values);
    } catch (error) {
      if (error instanceof RolesRequestError && error.status === 422 && Object.keys(error.fieldErrors).length > 0) {
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          form.setError(field as keyof RoleFormInput, {
            message: errors.has(error.code) ? errors(error.code) : messages.join(" "),
          });
        }
        return;
      }
      const code = error instanceof RolesRequestError ? error.code : "generic";
      form.setError("root.server", {
        message: errors.has(code) ? errors(code) : errors("generic"),
      });
    }
  };

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <section role="dialog" aria-modal="true" aria-labelledby="role-form-title" className="w-full max-w-xl rounded-2xl border border-border bg-card p-6 shadow-xl">
        <h2 id="role-form-title" className="text-xl font-semibold">{role ? t("formEditTitle") : t("formCreateTitle")}</h2>
        <form className="mt-5 space-y-4" onSubmit={form.handleSubmit(submit)}>
          <label className="block space-y-1.5 text-sm">
            <span>{t("name")}</span>
            <input autoFocus aria-label={t("name")} disabled={role?.isSystem} className="h-10 w-full rounded-lg border border-input bg-background px-3 disabled:opacity-60" {...form.register("name")} />
            {role?.isSystem && <span className="block text-xs text-muted-foreground">{t("baseName")}</span>}
            {form.formState.errors.name && <span role="alert" className="text-destructive">{validation(form.formState.errors.name.message ?? "requiredName")}</span>}
          </label>
          <label className="block space-y-1.5 text-sm">
            <span>{t("descriptionColumn")}</span>
            <textarea aria-label={t("descriptionColumn")} rows={3} className="w-full rounded-lg border border-input bg-background px-3 py-2" {...form.register("description")} />
            {form.formState.errors.description && <span role="alert" className="text-destructive">{validation(form.formState.errors.description.message ?? "roleDescriptionTooLong")}</span>}
          </label>
          {!role && <label className="block space-y-1.5 text-sm">
            <span>{t("copyPermissions")}</span>
            <select aria-label={t("copyPermissions")} className="h-10 w-full rounded-lg border border-input bg-background px-3" {...form.register("copyPermissionsFromRoleId")}>
              <option value="">{t("noCopy")}</option>
              {roles.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </select>
            {form.formState.errors.copyPermissionsFromRoleId && <span role="alert" className="text-destructive">{validation(form.formState.errors.copyPermissionsFromRoleId.message ?? "invalidRole")}</span>}
          </label>}
          {form.formState.errors.root?.server?.message && <p role="alert" className="text-sm text-destructive">{form.formState.errors.root.server.message}</p>}
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={onClose}>{common("cancel")}</Button>
            <Button type="submit" disabled={form.formState.isSubmitting}>{form.formState.isSubmitting ? common("saving") : common("save")}</Button>
          </div>
        </form>
      </section>
    </div>
  );
}
