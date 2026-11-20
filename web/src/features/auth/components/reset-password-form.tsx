"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { FieldLabel } from "@/components/ui/field-label";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { resetPasswordSchema } from "@/features/auth/schemas/auth.schema";
import { fetchBff } from "@/lib/api/browser-client";
import { useTranslations } from "next-intl";

export function ResetPasswordForm({
  email: initialEmail,
  token,
}: Readonly<{ email: string; token: string }>) {
  const t = useTranslations("auth.reset");
  const validation = useTranslations("auth.validation");
  const apiErrors = useTranslations("errors");
  const [email, setEmail] = useState(initialEmail);
  const [newPassword, setNewPassword] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const parsed = resetPasswordSchema.safeParse({ email, token, newPassword });
    if (!parsed.success) {
      setError(validation(parsed.error.issues[0]?.message ?? "invalidResetLink"));
      return;
    }
    setPending(true);
    setError("");
    try {
      const response = await fetchBff("/api/auth/reset-password", {
        method: "POST",
        body: JSON.stringify(parsed.data),
      });
      const body = await response.json();
      if (!response.ok) {
        setError(body.code && apiErrors.has(body.code) ? apiErrors(body.code) : t("requestFailed"));
        return;
      }
      setMessage(t("success"));
    } catch {
      setError(t("connectionError"));
    } finally {
      setPending(false);
    }
  }

  return (
    <form className="space-y-5" onSubmit={submit}>
      {error && <p role="alert" className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{error}</p>}
      {message && <p className="rounded-lg bg-emerald-50 p-3 text-sm text-emerald-800">{message}</p>}
      <div className="space-y-2">
        <FieldLabel htmlFor="email">{t("email")}</FieldLabel>
        <Input id="email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
      </div>
      <div className="space-y-2">
        <FieldLabel htmlFor="newPassword">{t("newPassword")}</FieldLabel>
        <PasswordInput id="newPassword" autoComplete="new-password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} />
      </div>
      <Button className="w-full" type="submit" disabled={pending}>
        {pending ? t("submitting") : t("submit")}
      </Button>
    </form>
  );
}
