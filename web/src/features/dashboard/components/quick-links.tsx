import Link from "next/link";
import { ArrowUpRight } from "lucide-react";
import type { SessionModule } from "@/features/auth/types/session";
import { getModuleIcon } from "@/lib/icon-map";
import { getTranslations } from "next-intl/server";

export async function QuickLinks({ modules }: Readonly<{ modules: SessionModule[] }>) {
  const t = await getTranslations("dashboard");
  const moduleNames = await getTranslations("layout.module");
  const links = modules
    .filter((module) => module.key !== "dashboard")
    .sort((left, right) => Number(left.sortOrder) - Number(right.sortOrder));

  return (
    <section>
      <div className="mb-4">
        <h2 className="font-semibold">{t("quickLinks")}</h2>
        <p className="mt-1 text-sm text-muted-foreground">{t("enabledModules")}</p>
      </div>
      {links.length === 0 ? (
        <p className="rounded-xl border border-dashed border-border p-6 text-sm text-muted-foreground">
          {t("noModules")}
        </p>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {links.map((module) => {
            const Icon = getModuleIcon(module.icon);
            return (
              <Link
                href={module.route ?? `/dashboard/${module.key}`}
                key={module.key}
                className="group flex items-center gap-4 rounded-2xl border border-border bg-card p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-md"
              >
                <span className="grid size-11 place-items-center rounded-xl bg-primary/10 text-primary">
                  <Icon className="size-5" />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block font-medium">{moduleNames.has(module.key) ? moduleNames(module.key) : module.name}</span>
                  <span className="mt-1 block text-sm text-muted-foreground">{t("openModule")}</span>
                </span>
                <ArrowUpRight className="size-4 text-muted-foreground transition group-hover:text-primary" />
              </Link>
            );
          })}
        </div>
      )}
    </section>
  );
}
