"use client";

import Image from "next/image";
import { useEffect, useState } from "react";
import QRCode from "qrcode";
import { Button } from "@/components/ui/button";
import { FieldLabel } from "@/components/ui/field-label";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { fetchBff } from "@/lib/api/browser-client";
import { notify } from "@/lib/notify";
import { useTranslations } from "next-intl";

async function request(path: string, body?: object) {
  const response = await fetchBff(path, { method: "POST", ...(body ? { body: JSON.stringify(body) } : {}) });
  const value = await response.json().catch(() => null) as { code?: string } | null;
  if (!response.ok) throw new Error(value?.code ?? "generic");
  return value;
}

export function TwoFactorSection() {
  const t = useTranslations("twoFactor");
  const errors = useTranslations("errors");
  const [setup, setSetup] = useState<{ secret: string; qr: string } | null>(null);
  const [code, setCode] = useState("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[] | null>(null);
  const [enabled, setEnabled] = useState(false);
  const [loaded, setLoaded] = useState(false);
  const [currentPassword, setCurrentPassword] = useState("");
  const [disableCode, setDisableCode] = useState("");
  useEffect(() => {
    void fetchBff("/api/auth/2fa").then(async (response) => {
      if (response.ok) setEnabled((await response.json() as { enabled: boolean }).enabled);
    }).finally(() => setLoaded(true));
  }, []);
  const showError = (error: unknown) => {
    const key = error instanceof Error ? error.message : "generic";
    notify.error(errors.has(key) ? errors(key) : errors("generic"));
  };
  const begin = async () => {
    try {
      const result = await request("/api/auth/2fa/setup") as { secret: string; otpauthUri: string };
      setSetup({ secret: result.secret, qr: await QRCode.toDataURL(result.otpauthUri, { width: 220, margin: 1 }) });
    } catch (error) { showError(error); }
  };
  const enable = async () => {
    try {
      const result = await request("/api/auth/2fa/enable", { code }) as { recoveryCodes: string[] };
      setRecoveryCodes(result.recoveryCodes); setSetup(null); setCode(""); setEnabled(true); notify.success(t("enabled"));
    } catch (error) { showError(error); }
  };
  const disable = async () => {
    try { await request("/api/auth/2fa/disable", { currentPassword, code: disableCode }); setEnabled(false); setCurrentPassword(""); setDisableCode(""); notify.success(t("disabled")); } catch (error) { showError(error); }
  };
  if (!loaded) return <section className="rounded-xl border border-border p-5"><p className="text-sm text-muted-foreground">{t("loading")}</p></section>;
  if (recoveryCodes) return <section className="rounded-xl border border-border p-5"><h2 className="font-semibold">{t("recoveryTitle")}</h2><p className="mt-1 text-sm text-muted-foreground">{t("recoveryWarning")}</p><code className="my-4 grid grid-cols-2 gap-2 rounded-lg bg-muted p-4 text-sm">{recoveryCodes.map((item) => <span key={item}>{item}</span>)}</code><Button onClick={() => setRecoveryCodes(null)}>{t("done")}</Button></section>;
  if (setup) return <section className="rounded-xl border border-border p-5"><h2 className="font-semibold">{t("setupTitle")}</h2><p className="mt-1 text-sm text-muted-foreground">{t("scan")}</p><Image src={setup.qr} width={220} height={220} alt={t("qrAlt")} unoptimized className="mt-4" /><p className="mt-3 break-all font-mono text-xs">{setup.secret}</p><div className="mt-4 max-w-sm space-y-2"><FieldLabel htmlFor="two-factor-code">{t("code")}</FieldLabel><Input id="two-factor-code" inputMode="numeric" autoComplete="one-time-code" value={code} onChange={(event) => setCode(event.target.value)} /><div className="flex gap-2"><Button onClick={enable}>{t("activate")}</Button><Button variant="outline" onClick={() => setSetup(null)}>{t("cancel")}</Button></div></div></section>;
  if (enabled) return <section className="rounded-xl border border-border p-5"><h2 className="font-semibold">{t("activeTitle")}</h2><p className="mt-1 text-sm text-muted-foreground">{t("activeDescription")}</p><div className="mt-4 grid max-w-sm gap-3"><PasswordInput autoComplete="current-password" placeholder={t("currentPassword")} value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} /><Input inputMode="numeric" autoComplete="one-time-code" placeholder={t("code")} value={disableCode} onChange={(event) => setDisableCode(event.target.value)} /><Button variant="destructive" onClick={disable}>{t("disable")}</Button></div></section>;
  return <section className="rounded-xl border border-border p-5"><h2 className="font-semibold">{t("title")}</h2><p className="mt-1 text-sm text-muted-foreground">{t("description")}</p><Button className="mt-4" onClick={begin}>{t("setup")}</Button></section>;
}
