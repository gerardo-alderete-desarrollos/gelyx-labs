import Link from "next/link";
import { ShieldAlert } from "lucide-react";
import { getTranslations } from "next-intl/server";

export default async function ForbiddenPage() {
  const t = await getTranslations("pages.forbidden");
  return (
    <main className="grid min-h-screen place-items-center px-6 py-12">
      <div className="max-w-md text-center">
        <span className="mx-auto grid size-14 place-items-center rounded-2xl bg-amber-100 text-amber-700"><ShieldAlert className="size-7" /></span>
        <p className="mt-6 text-sm font-semibold uppercase tracking-wider text-amber-700">{t("eyebrow")}</p>
        <h1 className="mt-2 text-3xl font-semibold">{t("title")}</h1>
        <p className="mt-3 text-muted-foreground">{t("description")}</p>
        <Link href="/dashboard" className="mt-7 inline-flex rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground">{t("back")}</Link>
      </div>
    </main>
  );
}
