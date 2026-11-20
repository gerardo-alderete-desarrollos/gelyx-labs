import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { ChangePasswordForm } from "@/features/profile/components/change-password-form";
import { getSession } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("pages.changePassword");
  return { title: t("metadata") };
}

export default async function ChangePasswordPage() {
  const t = await getTranslations("auth.changePassword");
  const session = await getSession("/change-password");
  if (!session.mustChangePassword && !session.permissions.includes("profile.update")) {
    redirect("/403");
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-xl items-center px-5 py-12">
      <section className="w-full rounded-2xl border border-border bg-card p-6 shadow-sm md:p-8">
        <p className="text-sm font-medium text-primary">{t("eyebrow")}</p>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">
          {session.mustChangePassword ? t("firstTitle") : t("title")}
        </h1>
        <p className="mt-2 mb-7 text-muted-foreground">
          {session.mustChangePassword
            ? t("firstDescription")
            : t("description")}
        </p>
        <ChangePasswordForm />
      </section>
    </main>
  );
}
