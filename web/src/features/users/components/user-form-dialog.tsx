"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { userFormSchema, type RoleOption, type UserFormValues } from "@/features/users/schemas/users.schema";
import { ApiRequestError } from "@/features/users/services/users.service";
import { Button } from "@/components/ui/button";
import { useTranslations } from "next-intl";

export function UserFormDialog({
  roles,
  onClose,
  onSubmit,
}: Readonly<{
  roles: RoleOption[];
  onClose: () => void;
  onSubmit: (values: UserFormValues) => Promise<void>;
}>) {
  const t = useTranslations("users");
  const validation = useTranslations("auth.validation");
  const errors = useTranslations("errors");
  const form = useForm<UserFormValues>({
    resolver: zodResolver(userFormSchema),
    defaultValues: { email: "", fullName: "", roleIds: [] },
  });
  const submit = async (values: UserFormValues) => {
    try {
      await onSubmit(values);
    } catch (error) {
      if (error instanceof ApiRequestError && error.status === 422 && Object.keys(error.fieldErrors).length > 0) {
        for (const [field, messages] of Object.entries(error.fieldErrors)) {
          form.setError(field as keyof UserFormValues, {
            message: errors.has(error.code) ? errors(error.code) : messages.join(" "),
          });
        }
        return;
      }
      const code = error instanceof ApiRequestError ? error.code : "generic";
      form.setError("root.server", { message: errors.has(code) ? errors(code) : errors("generic") });
    }
  };

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4" onMouseDown={(event) => {
      if (event.target === event.currentTarget) onClose();
    }}>
      <section role="dialog" aria-modal="true" aria-labelledby="create-user-title" className="w-full max-w-xl rounded-2xl border border-border bg-card p-6 shadow-xl">
        <h2 id="create-user-title" className="text-xl font-semibold">{t("createDialogTitle")}</h2>
        <p className="mt-1 text-sm text-muted-foreground">{t("temporaryPasswordNotice")}</p>
        <form className="mt-5 space-y-4" onSubmit={form.handleSubmit(submit)}>
          <label className="block space-y-1.5 text-sm">
            <span>{t("fullName")}</span>
            <input autoFocus aria-label={t("fullName")} className="h-10 w-full rounded-lg border border-input bg-background px-3" {...form.register("fullName")} />
            {form.formState.errors.fullName && <span role="alert" className="text-destructive">{validation(form.formState.errors.fullName.message ?? "requiredName")}</span>}
          </label>
          <label className="block space-y-1.5 text-sm">
            <span>{t("email")}</span>
            <input type="email" aria-label={t("email")} className="h-10 w-full rounded-lg border border-input bg-background px-3" {...form.register("email")} />
            {form.formState.errors.email && <span role="alert" className="text-destructive">{validation(form.formState.errors.email.message ?? "invalidEmail")}</span>}
          </label>
          <fieldset className="space-y-2">
            <legend className="text-sm font-medium">{t("roles")}</legend>
            <div className="grid gap-2 sm:grid-cols-2">
              {roles.map((role) => (
                <label key={role.id} className="flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm">
                  <input type="checkbox" value={role.id} {...form.register("roleIds")} />
                  {role.name}
                </label>
              ))}
            </div>
            {form.formState.errors.roleIds && <p role="alert" className="text-sm text-destructive">{validation(form.formState.errors.roleIds.message ?? "requiredRole")}</p>}
          </fieldset>
          {form.formState.errors.root?.server?.message && <p role="alert" className="text-sm text-destructive">{form.formState.errors.root.server.message}</p>}
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={onClose}>{t("cancel")}</Button>
            <Button type="submit" disabled={form.formState.isSubmitting}>
              {form.formState.isSubmitting ? t("creating") : t("create")}
            </Button>
          </div>
        </form>
      </section>
    </div>
  );
}
