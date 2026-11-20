"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { FieldLabel } from "@/components/ui/field-label";
import { PasswordInput } from "@/components/ui/password-input";
import { changePasswordSchema, type ChangePasswordFormValues } from "@/features/auth/schemas/auth.schema";
import { fetchBff } from "@/lib/api/browser-client";
import { useTranslations } from "next-intl";

export function ChangePasswordForm() {
  const t = useTranslations("auth.changePassword");
  const validation = useTranslations("auth.validation");
  const apiErrors = useTranslations("errors");
  const router = useRouter();
  const queryClient = useQueryClient();
  const [serverError, setServerError] = useState("");
  const {
    register,
    handleSubmit,
    formState: { errors: fieldErrors, isSubmitting },
  } = useForm<ChangePasswordFormValues>({ resolver: zodResolver(changePasswordSchema) });

  async function submit(values: ChangePasswordFormValues) {
    setServerError("");
    try {
      const response = await fetchBff("/api/auth/change-password", {
        method: "POST",
        body: JSON.stringify({
          currentPassword: values.currentPassword,
          newPassword: values.newPassword,
          confirmPassword: values.confirmPassword,
        }),
      });
      if (!response.ok) {
        const body = (await response.json().catch(() => null)) as { code?: string } | null;
        setServerError(body?.code && apiErrors.has(body.code) ? apiErrors(body.code) : apiErrors("generic"));
        return;
      }

      queryClient.clear();
      const channel = new BroadcastChannel("gelyx-labs-session");
      channel.postMessage("password-changed");
      channel.close();
      router.replace("/login?reason=password-changed");
      router.refresh();
    } catch {
      setServerError(t("connectionError"));
      return;
    }
  }

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-5">
      {serverError && <p role="alert" className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{serverError}</p>}
      {([
        ["currentPassword", t("currentPassword"), "current-password"],
        ["newPassword", t("newPassword"), "new-password"],
        ["confirmPassword", t("confirmPassword"), "new-password"],
      ] as const).map(([name, label, autoComplete]) => (
        <div className="space-y-2" key={name}>
          <FieldLabel htmlFor={name}>{label}</FieldLabel>
          <PasswordInput id={name} autoComplete={autoComplete} {...register(name)} />
          {fieldErrors[name] && <p className="text-sm text-destructive">{validation(fieldErrors[name]?.message ?? "requiredPassword")}</p>}
        </div>
      ))}
      <p className="text-xs text-muted-foreground">{t("minimumLength")}</p>
      <Button type="submit" disabled={isSubmitting}>{isSubmitting ? t("submitting") : t("submit")}</Button>
    </form>
  );
}