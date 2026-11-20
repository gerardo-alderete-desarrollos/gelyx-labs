import { ArrowDownRight, ArrowUpRight, UsersRound } from "lucide-react";
import type { UsersSummary } from "@/features/dashboard/types/dashboard";
import { useLocale, useTranslations } from "next-intl";

export function SummaryCards({ summary }: Readonly<{ summary: UsersSummary }>) {
  const locale = useLocale();
  const t = useTranslations("dashboard");
  const cards = [
    { key: "total", label: t("totalUsers"), icon: UsersRound, note: t("registeredAccounts") },
    { key: "active", label: t("activeUsers"), icon: ArrowUpRight, note: t("enabledAccess") },
    { key: "inactive", label: t("inactiveUsers"), icon: ArrowDownRight, note: t("temporaryAccess") },
  ] as const;
  return (
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      {cards.map(({ key, label, icon: Icon, note }) => (
        <article key={key} className="rounded-2xl border border-border bg-card p-5 shadow-sm">
          <div className="flex items-center justify-between">
            <p className="text-sm font-medium text-muted-foreground">{label}</p>
            <span className="grid size-10 place-items-center rounded-xl bg-primary/10 text-primary"><Icon className="size-5" /></span>
          </div>
          <p className="mt-5 text-3xl font-semibold tracking-tight">{Number(summary[key]).toLocaleString(locale)}</p>
          <p className="mt-1 text-sm text-muted-foreground">{note}</p>
        </article>
      ))}
    </div>
  );
}
