"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Clock3, Globe2, LaptopMinimal, ShieldCheck } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import {
  getActiveSessions,
  revokeOtherSessions,
  revokeSession,
} from "@/features/sessions/services/sessions.service";
import type { ActiveSession } from "@/features/sessions/types/session";
import { notify } from "@/lib/notify";

export const activeSessionsQueryKey = ["auth", "sessions"] as const;

function formatDate(value: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

export function SessionsSection() {
  const t = useTranslations("sessions");
  const locale = useLocale();
  const queryClient = useQueryClient();
  const sessionsQuery = useQuery({
    queryKey: activeSessionsQueryKey,
    queryFn: getActiveSessions,
  });

  const refreshSessions = () => queryClient.invalidateQueries({ queryKey: activeSessionsQueryKey });
  const revokeMutation = useMutation({
    mutationFn: (session: ActiveSession) => revokeSession(session.id),
    onSuccess: async () => {
      await refreshSessions();
      notify.success(t("sessionClosed"));
    },
    onError: notify.error,
  });
  const revokeOthersMutation = useMutation({
    mutationFn: revokeOtherSessions,
    onSuccess: async ({ revokedCount }) => {
      await refreshSessions();
      notify.success(t("otherSessionsClosed", { count: revokedCount }));
    },
    onError: notify.error,
  });

  const sessions = sessionsQuery.data ?? [];
  const hasOtherSessions = sessions.some((session) => !session.current);

  return (
    <section className="rounded-2xl border border-border bg-card p-6 shadow-sm md:p-8">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
        <div>
          <div className="flex items-center gap-2">
            <ShieldCheck className="size-5 text-primary" />
            <h2 className="font-semibold">{t("title")}</h2>
          </div>
          <p className="mt-1 text-sm text-muted-foreground">{t("description")}</p>
        </div>
        <Button
          variant="outline"
          onClick={() => revokeOthersMutation.mutate()}
          disabled={!hasOtherSessions || revokeOthersMutation.isPending}
        >
          {revokeOthersMutation.isPending ? t("closingOthers") : t("closeOthers")}
        </Button>
      </div>

      {sessionsQuery.isPending ? (
        <div className="mt-5 h-32 animate-pulse rounded-xl bg-muted" aria-label={t("loading")} />
      ) : sessionsQuery.isError ? (
        <p role="alert" className="mt-5 rounded-xl bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {t("loadError")}
        </p>
      ) : sessions.length === 0 ? (
        <p className="mt-5 rounded-xl bg-muted/60 px-4 py-8 text-center text-sm text-muted-foreground">{t("empty")}</p>
      ) : (
        <ul className="mt-5 divide-y divide-border">
          {sessions.map((session) => {
            const label = session.agent ?? session.createdByIp ?? t("unknownDevice");
            return (
              <li key={session.id} className="flex flex-col gap-4 py-4 first:pt-0 last:pb-0 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex min-w-0 gap-3">
                  <span className="grid size-9 shrink-0 place-items-center rounded-lg bg-muted text-muted-foreground">
                    <LaptopMinimal className="size-4" />
                  </span>
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="truncate text-sm font-medium" title={label}>{label}</p>
                      {session.current && <span className="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">{t("current")}</span>}
                    </div>
                    <div className="mt-1 flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
                      <span className="inline-flex items-center gap-1"><Globe2 className="size-3" />{session.createdByIp ?? t("unknownIp")}</span>
                      <time className="inline-flex items-center gap-1" dateTime={session.createdAt}>
                        <Clock3 className="size-3" />{t("startedAt", { date: formatDate(session.createdAt, locale) })}
                      </time>
                    </div>
                  </div>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => revokeMutation.mutate(session)}
                  disabled={revokeMutation.isPending}
                  aria-label={t("closeSessionAria", { session: label })}
                >
                  {revokeMutation.isPending && revokeMutation.variables?.id === session.id ? t("closing") : t("close")}
                </Button>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
