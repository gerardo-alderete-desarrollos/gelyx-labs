"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { loginSchema, type LoginFormValues } from "@/features/auth/schemas/auth.schema";
import { login } from "@/features/auth/services/login";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { FieldLabel } from "@/components/ui/field-label";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { safeReturnUrl } from "@/lib/auth/return-url";
import { useTranslations } from "next-intl";

const REMEMBERED_EMAIL_KEY = "auth.rememberedEmail";

function getRememberedEmail(): string {
  if (typeof window === "undefined") return "";
  try {
    return localStorage.getItem(REMEMBERED_EMAIL_KEY) ?? "";
  } catch {
    return "";
  }
}

function setRememberedEmail(email: string): void {
  if (typeof window === "undefined") return;
  try {
    if (email) localStorage.setItem(REMEMBERED_EMAIL_KEY, email);
    else localStorage.removeItem(REMEMBERED_EMAIL_KEY);
  } catch {
    // ignore
  }
}

export function LoginForm({
  returnUrl,
  reason,
}: Readonly<{ returnUrl: string | null; reason: string | null }>) {
  const router = useRouter();
  const t = useTranslations("auth.login");
  const validation = useTranslations("auth.validation");
  const apiErrors = useTranslations("errors");
  const [twoFactorToken, setTwoFactorToken] = useState("");
  const [twoFactorCode, setTwoFactorCode] = useState("");
  const mutation = useMutation({
    mutationFn: login,
    onSuccess: (session) => {
      if ("twoFactorRequired" in session) { setTwoFactorToken(session.twoFactorToken); return; }
      router.replace(
        session.mustChangePassword ? "/change-password" : safeReturnUrl(returnUrl),
      );
      router.refresh();
    },
  });
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: getRememberedEmail(), password: "", rememberMe: !!getRememberedEmail() },
  });

  const notice = reason === "session-expired"
    ? t("reasonSessionExpired")
    : reason === "account-inactive"
      ? t("reasonAccountInactive")
      : reason === "password-changed"
        ? t("reasonPasswordChanged")
        : reason === "signed-out"
          ? t("reasonSignedOut")
          : reason === "logout-failed"
            ? t("reasonLogoutFailed")
          : null;

  const rememberMe = watch("rememberMe");
  useEffect(() => {
    if (rememberMe) setRememberedEmail(watch("email"));
    else setRememberedEmail("");
  }, [rememberMe, watch]);

  async function submitTwoFactor() {
    const response = await fetch("/api/auth/login/2fa", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ twoFactorToken, code: twoFactorCode }) });
    if (!response.ok) { const body = await response.json().catch(() => null) as { code?: string } | null; throw new Error(body?.code ?? "generic"); }
    const session = await response.json() as { mustChangePassword: boolean };
    router.replace(session.mustChangePassword ? "/change-password" : safeReturnUrl(returnUrl)); router.refresh();
  }

  if (twoFactorToken) return <form className="space-y-5" onSubmit={(event) => { event.preventDefault(); void submitTwoFactor().catch(() => {}); }}><p className="text-sm text-muted-foreground">{t("twoFactorDescription")}</p><div className="space-y-2"><FieldLabel htmlFor="two-factor-login-code">{t("twoFactorCode")}</FieldLabel><Input id="two-factor-login-code" autoComplete="one-time-code" inputMode="numeric" value={twoFactorCode} onChange={(event) => setTwoFactorCode(event.target.value)} /></div><Button className="h-11 w-full" type="submit">{t("twoFactorSubmit")}</Button></form>;

  return (
    <form className="space-y-5" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
      {notice && <p className="rounded-lg bg-blue-50 px-4 py-3 text-sm text-blue-800">{notice}</p>}
      {mutation.error && (
        <p role="alert" className="rounded-lg bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {apiErrors.has(mutation.error.message) ? apiErrors(mutation.error.message) : apiErrors("generic")}
        </p>
      )}
      <div className="space-y-2">
        <FieldLabel htmlFor="email">{t("email")}</FieldLabel>
        <Input id="email" type="email" autoComplete="username" {...register("email")} />
        {errors.email && <p className="text-sm text-destructive">{validation(errors.email.message ?? "invalidEmail")}</p>}
      </div>
      <div className="space-y-2">
        <FieldLabel htmlFor="password">{t("password")}</FieldLabel>
        <PasswordInput id="password" autoComplete="current-password" {...register("password")} />
        {errors.password && <p className="text-sm text-destructive">{validation(errors.password.message ?? "requiredPassword")}</p>}
      </div>
      <div className="flex items-center gap-2">
        <Checkbox id="rememberMe" {...register("rememberMe")} />
        <label htmlFor="rememberMe" className="text-sm">{t("rememberMe")}</label>
      </div>
      <div className="flex justify-end">
        <a href="/forgot-password" className="text-sm font-medium text-primary hover:underline">
          {t("forgotPassword")}
        </a>
      </div>
      <Button className="h-11 w-full" type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? t("submitting") : t("submit")}
      </Button>
    </form>
  );
}
