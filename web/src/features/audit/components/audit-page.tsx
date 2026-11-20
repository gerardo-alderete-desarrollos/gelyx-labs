"use client";

import { useEffect, useMemo, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { getAuditLogs } from "@/features/audit/services/audit.service";
import { useLocale, useTranslations } from "next-intl";
import { Can } from "@/features/auth/components/can";
import { ExportCsvButton } from "@/components/export-csv-button";

export function AuditPage({ initialQuery }: Readonly<{ initialQuery: Record<string, string | undefined> }>) {
  const t = useTranslations("audit");
  const common = useTranslations("common");
  const errors = useTranslations("errors");
  const locale = useLocale();
  const router = useRouter();
  const pathname = usePathname();
  const [action, setAction] = useState(initialQuery.action ?? "");
  const [entityType, setEntityType] = useState(initialQuery.entityType ?? "");
  const [actorUserId, setActorUserId] = useState(initialQuery.actorUserId ?? "");
  const page = Math.max(1, Number(initialQuery.page) || 1);
  const pageSize = Math.min(100, Math.max(1, Number(initialQuery.pageSize) || 20));
  const params = useMemo(() => {
    const next = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (action) next.set("action", action);
    if (entityType) next.set("entityType", entityType);
    if (actorUserId) next.set("actorUserId", actorUserId);
    return next;
  }, [action, actorUserId, entityType, page, pageSize]);
  const logs = useQuery({ queryKey: ["audit-logs", params.toString()], queryFn: () => getAuditLogs(params) });

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      const search = new URLSearchParams();
      search.set("page", "1");
      search.set("pageSize", String(pageSize));
      if (action.trim()) search.set("action", action.trim());
      if (entityType.trim()) search.set("entityType", entityType.trim());
      if (actorUserId.trim()) search.set("actorUserId", actorUserId.trim());
      const matchesInitial =
        action.trim() === (initialQuery.action ?? "") &&
        entityType.trim() === (initialQuery.entityType ?? "") &&
        actorUserId.trim() === (initialQuery.actorUserId ?? "");
      if (!matchesInitial) router.replace(`${pathname}?${search.toString()}`);
    }, 300);
    return () => window.clearTimeout(timeout);
  }, [action, actorUserId, entityType, initialQuery.action, initialQuery.actorUserId, initialQuery.entityType, pageSize, pathname, router]);

  const movePage = (nextPage: number) => {
    const next = new URLSearchParams(params);
    next.set("page", String(nextPage));
    router.replace(`${pathname}?${next}`);
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end"><div><p className="text-sm font-medium text-primary">{common("administration")}</p><h1 className="mt-1 text-3xl font-semibold tracking-tight">{t("title")}</h1><p className="mt-2 text-muted-foreground">{t("description")}</p></div><Can permission="audit.read"><ExportCsvButton path={`/api/audit-logs/export?${params}`} label={t("export")} loadingLabel={t("exporting")} errorMessage={t("exportError")} fallbackFileName="audit-logs.csv" /></Can></header>
      <section className="grid gap-3 rounded-2xl border border-border bg-card p-4 sm:grid-cols-3">
        <label className="space-y-1 text-sm"><span>{t("action")}</span><input aria-label={t("filterAction")} className="h-10 w-full rounded-lg border border-input bg-background px-3" placeholder={t("actionPlaceholder")} value={action} onChange={(event) => setAction(event.target.value)} /></label>
        <label className="space-y-1 text-sm"><span>{t("entity")}</span><input aria-label={t("filterEntity")} className="h-10 w-full rounded-lg border border-input bg-background px-3" placeholder={t("entityPlaceholder")} value={entityType} onChange={(event) => setEntityType(event.target.value)} /></label>
        <label className="space-y-1 text-sm"><span>{t("actor")}</span><input aria-label={t("filterActor")} className="h-10 w-full rounded-lg border border-input bg-background px-3" placeholder={t("actorPlaceholder")} value={actorUserId} onChange={(event) => setActorUserId(event.target.value)} /></label>
      </section>
      {logs.isPending ? <div className="h-56 animate-pulse rounded-2xl bg-muted" aria-label={t("loading")} /> : logs.isError ? (
        <section role="alert" className="rounded-2xl border border-destructive/30 p-6 text-destructive">{errors.has(logs.error.message) ? errors(logs.error.message) : errors("generic")} <Button variant="outline" onClick={() => logs.refetch()}>{common("retry")}</Button></section>
      ) : logs.data.items.length === 0 ? (
        <section className="rounded-2xl border border-dashed border-border bg-card p-10 text-center"><h2 className="font-semibold">{t("empty")}</h2><p className="mt-2 text-sm text-muted-foreground">{t("emptyDescription")}</p></section>
      ) : (
        <>
          <div className="overflow-x-auto rounded-2xl border border-border bg-card"><table className="w-full min-w-[760px] text-left text-sm"><thead className="bg-muted/60 text-muted-foreground"><tr>{[t("date"), t("action"), t("entity"), t("actor"), t("ip"), t("metadataColumn")].map((label) => <th key={label} scope="col" className="px-4 py-3 font-medium">{label}</th>)}</tr></thead><tbody className="divide-y divide-border">{logs.data.items.map((log) => <tr key={log.id}><td className="px-4 py-3 whitespace-nowrap">{new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" }).format(new Date(log.createdAt))}</td><td className="px-4 py-3 font-medium">{log.action}</td><td className="px-4 py-3">{log.entityType}{log.entityId && <span className="block text-xs text-muted-foreground">{log.entityId}</span>}</td><td className="px-4 py-3 font-mono text-xs">{log.actorUserId ?? t("system")}</td><td className="px-4 py-3">{log.ip ?? "—"}</td><td className="px-4 py-3"><details><summary className="cursor-pointer text-primary">{t("viewData")}</summary><pre className="mt-2 max-w-sm overflow-auto whitespace-pre-wrap break-all rounded bg-muted p-2 text-xs">{JSON.stringify(log.metadata, null, 2)}</pre></details></td></tr>)}</tbody></table></div>
          <div className="flex flex-col justify-between gap-3 text-sm text-muted-foreground sm:flex-row sm:items-center"><span>{t("count", { count: new Intl.NumberFormat(locale).format(logs.data.totalCount), page: logs.data.page, total: Math.max(1, logs.data.totalPages) })}</span><div className="flex gap-2"><Button variant="outline" disabled={page <= 1} onClick={() => movePage(page - 1)}>{common("previous")}</Button><Button variant="outline" disabled={page >= logs.data.totalPages} onClick={() => movePage(page + 1)}>{common("next")}</Button></div></div>
        </>
      )}
    </div>
  );
}
