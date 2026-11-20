"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { FieldLabel } from "@/components/ui/field-label";
import { Input } from "@/components/ui/input";
import { forgotPasswordSchema } from "@/features/auth/schemas/auth.schema";
import { fetchBff } from "@/lib/api/browser-client";
import { useTranslations } from "next-intl";

export function ForgotPasswordForm() {
  const t = useTranslations("auth.forgot");
  const validation = useTranslations("auth.validation");
  const apiErrors = useTranslations("errors");
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setMessage("");
    const parsed = forgotPasswordSchema.safeParse({ email });
    if (!parsed.success) {
      setError(validation(parsed.error.issues[0]?.message ?? "invalidEmail"));
      return;
    }

    setPending(true);
    try {
      const response = await fetchBff("/api/auth/forgot-password", {
        method: "POST",
        body: JSON.stringify(parsed.data),
      });
      const body = await response.json();
      if (!response.ok) {
        setError(body.code && apiErrors.has(body.code) ? apiErrors(body.code) : t("requestFailed"));
        return;
      }
      setMessage(t("sent"));
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
        <Input id="email" type="email" autoComplete="email" value={email} onChange={(event) => setEmail(event.target.value)} />
      </div>
      <Button className="w-full" type="submit" disabled={pending}>
        {pending ? t("submitting") : t("submit")}
      </Button>
    </form>
  );
}
