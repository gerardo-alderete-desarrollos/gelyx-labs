import { Clock3 } from "lucide-react";
import type { UsersSummary } from "@/features/dashboard/types/dashboard";
import { useLocale, useTranslations } from "next-intl";

export function RecentLogins({ users }: Readonly<{ users: UsersSummary["recentLogins"] }>) {
  const locale = useLocale();
  const t = useTranslations("dashboard");
  return (
    <section className="rounded-2xl border border-border bg-card p-6 shadow-sm">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="font-semibold">{t("recentLogins")}</h2>
          <p className="mt-1 text-sm text-muted-foreground">{t("recentActivity")}</p>
        </div>
        <Clock3 className="size-5 text-muted-foreground" />
      </div>
      {users.length === 0 ? (
        <p className="mt-8 rounded-xl bg-muted/60 px-4 py-8 text-center text-sm text-muted-foreground">
          {t("noRecentLogins")}
        </p>
      ) : (
        <ul className="mt-5 divide-y divide-border">
          {users.map((user) => (
            <li key={user.id} className="flex items-center justify-between gap-4 py-4 first:pt-0 last:pb-0">
              <div className="min-w-0">
                <p className="truncate text-sm font-medium">{user.fullName ?? user.email}</p>
                <p className="truncate text-sm text-muted-foreground">{user.email}</p>
              </div>
              <time className="shrink-0 text-xs text-muted-foreground" dateTime={user.lastLoginAt}>
                {formatRelativeTime(new Date(user.lastLoginAt), locale)}
              </time>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function formatRelativeTime(date: Date, locale: string): string {
  const elapsedSeconds = Math.round((date.getTime() - Date.now()) / 1000);
  const units: [Intl.RelativeTimeFormatUnit, number][] = [
    ["year", 60 * 60 * 24 * 365],
    ["month", 60 * 60 * 24 * 30],
    ["week", 60 * 60 * 24 * 7],
    ["day", 60 * 60 * 24],
    ["hour", 60 * 60],
    ["minute", 60],
    ["second", 1],
  ];
  const [unit, seconds] = units.find(([, size]) => Math.abs(elapsedSeconds) >= size) ?? ["second", 1];
  return new Intl.RelativeTimeFormat(locale, { numeric: "auto" }).format(
    Math.round(elapsedSeconds / seconds),
    unit,
  );
}
