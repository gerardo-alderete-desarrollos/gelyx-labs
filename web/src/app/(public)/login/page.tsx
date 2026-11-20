import { LoginForm } from "@/features/auth/components/login-form";
import { safeReturnUrl } from "@/lib/auth/return-url";
import { LocaleSwitcher } from "@/components/layout/locale-switcher";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("auth.login");
  return { title: t("metadata") };
}

export default async function LoginPage({
  searchParams,
}: Readonly<{ searchParams: Promise<Record<string, string | string[] | undefined>> }>) {
  const t = await getTranslations("auth.login");
  const params = await searchParams;
  const reason =
    typeof params.reason === "string"
      ? params.reason
      : typeof params.returnUrl === "string" && params.returnUrl.includes("reason=session-expired")
        ? "session-expired"
        : null;
  const returnUrl = typeof params.returnUrl === "string" ? safeReturnUrl(params.returnUrl) : null;

  return (
    <div>
      <div className="mb-6 flex justify-end"><LocaleSwitcher /></div>
      <p className="text-sm font-medium text-primary">{t("welcome")}</p>
      <h2 className="mt-2 text-3xl font-semibold tracking-tight">{t("title")}</h2>
      <p className="mt-2 text-muted-foreground">{t("description")}</p>
      <div className="mt-8">
        <LoginForm returnUrl={returnUrl} reason={reason} />
      </div>
    </div>
  );
}
