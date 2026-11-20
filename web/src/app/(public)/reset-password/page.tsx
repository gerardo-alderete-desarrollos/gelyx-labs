import Link from "next/link";
import { ResetPasswordForm } from "@/features/auth/components/reset-password-form";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("auth.reset");
  return { title: t("metadata") };
}

export default async function ResetPasswordPage({
  searchParams,
}: Readonly<{ searchParams: Promise<Record<string, string | string[] | undefined>> }>) {
  const t = await getTranslations("auth.reset");
  const params = await searchParams;
  const email = typeof params.email === "string" ? params.email : "";
  const token = typeof params.token === "string" ? params.token : "";
  return (
    <div>
      <p className="text-sm font-medium text-primary">{t("eyebrow")}</p>
      <h1 className="mt-2 text-3xl font-semibold tracking-tight">{t("title")}</h1>
      <p className="mt-2 text-muted-foreground">{t("description")}</p>
      <div className="mt-8"><ResetPasswordForm email={email} token={token} /></div>
      <Link href="/login" className="mt-6 inline-block text-sm font-medium text-primary hover:underline">{t("backToLogin")}</Link>
    </div>
  );
}
