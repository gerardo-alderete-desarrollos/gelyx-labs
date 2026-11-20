import { notFound, redirect } from "next/navigation";
import { getSession } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export default async function ModulePlaceholderPage({
  params,
}: Readonly<{ params: Promise<{ module: string }> }>) {
  const { module } = await params;
  const t = await getTranslations("pages.module");
  const moduleNames = await getTranslations("layout.module");
  const session = await getSession(`/dashboard/${module}`);
  const definition = session.modules.find((item) => item.key === module);
  if (!definition) {
    notFound();
  }
  if (definition.route && definition.route !== `/dashboard/${module}`) {
    redirect(definition.route);
  }

  return (
    <section className="rounded-2xl border border-border bg-card p-8">
      <p className="text-sm font-medium text-primary">{t("enabled")}</p>
      <h1 className="mt-2 text-3xl font-semibold">{moduleNames.has(module) ? moduleNames(module) : definition.name}</h1>
      <p className="mt-3 text-muted-foreground">{t("comingSoon")}</p>
    </section>
  );
}
