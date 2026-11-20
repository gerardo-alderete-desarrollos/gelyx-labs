import Link from "next/link";
import { ForgotPasswordForm } from "@/features/auth/components/forgot-password-form";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("auth.forgot");
  return { title: t("metadata") };
}

export default async function ForgotPasswordPage() {
  const t = await getTranslations("auth.forgot");
  return (
    <div>
      <p className="text-sm font-medium text-primary">{t("eyebrow")}</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-2 text-muted-foreground">{t("description")}</p>
      <div className="mt-8"><ForgotPasswordForm /></div>
      <Link href="/login" className="mt-6 inline-block text-sm font-medium text-primary hover:underline">{t("backToLogin")}</Link>
    </div>
  );
}
