"use client";

import Link from "next/link";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { ModuleFormDialog } from "@/features/modules/components/module-form-dialog";
import { useModules } from "@/features/modules/hooks/use-modules";
import type { ManagedModule } from "@/features/modules/schemas/modules.schema";
import { modulesService } from "@/features/modules/services/modules.service";
import type { ModuleUpdateVariables } from "@/features/modules/types/modules.types";
import { notify } from "@/lib/notify";

const nonToggleableKeys = new Set(["dashboard", "profile"]);

function moduleName(module: ManagedModule, locale: string) {
  return module.translations.find((translation) => translation.locale === locale)?.name
    ?? module.translations[0]?.name
    ?? module.key;
}

export function ModulesPage() {
  const t = useTranslations("modules");
  const common = useTranslations("common");
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const modulesQuery = useModules();
  const [editingModule, setEditingModule] = useState<ManagedModule | null>(null);
  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["modules"] });
    await queryClient.invalidateQueries({ queryKey: ["session"] });
    router.refresh();
  };
  const updateMutation = useMutation({
    mutationFn: ({ module, values }: ModuleUpdateVariables) => modulesService.update(module.id, values),
    onSuccess: invalidate,
  });
  const stateMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => modulesService.updateState(id, isActive),
    onSuccess: invalidate,
  });
  const modules = modulesQuery.data ?? [];

  const changeState = async (module: ManagedModule) => {
    try {
      await stateMutation.mutateAsync({ id: module.id, isActive: !module.isActive });
      notify.success(t("stateSaved"));
    } catch (error) {
      notify.error(error);
    }
  };

  return (
    <div className="space-y-6">
      <header>
        <p className="text-sm font-medium text-primary">{common("administration")}</p>
        <h1 className="mt-1 text-3xl font-semibold tracking-tight">{t("title")}</h1>
        <p className="mt-2 text-muted-foreground">{t("description")}</p>
      </header>
      {modulesQuery.isPending ? <div className="h-64 animate-pulse rounded-2xl bg-muted" aria-label={t("loading")} /> : modulesQuery.isError ? (
        <section role="alert" className="rounded-2xl border border-destructive/30 bg-card p-8 text-center"><p className="text-destructive">{t("loadError")}</p><Button className="mt-4" variant="outline" onClick={() => void modulesQuery.refetch()}>{common("retry")}</Button></section>
      ) : (
        <div className="overflow-x-auto rounded-2xl border border-border bg-card">
          <table className="w-full text-left text-sm">
            <thead className="bg-muted/60 text-muted-foreground"><tr>{[t("nameColumn"), t("key"), t("state"), t("roles"), t("actions")].map((heading) => <th key={heading} scope="col" className="px-4 py-3 font-medium">{heading}</th>)}</tr></thead>
            <tbody className="divide-y divide-border">
              {modules.map((module) => {
                const stateLocked = module.isSystem || nonToggleableKeys.has(module.key);
                const stateLabel = module.isActive ? t("active") : t("inactive");
                return <tr key={module.key}>
                  <td className="px-4 py-4 font-medium">{moduleName(module, locale)}</td>
                  <td className="px-4 py-4 font-mono text-muted-foreground">{module.key}</td>
                  <td className="px-4 py-4">
                    <span title={stateLocked ? t("stateLocked") : undefined}>
                      <label className="inline-flex items-center gap-2">
                        <input aria-label={t("toggleState", { name: moduleName(module, locale) })} type="checkbox" checked={module.isActive} disabled={stateLocked || stateMutation.isPending} title={stateLocked ? t("stateLocked") : undefined} onChange={() => void changeState(module)} />
                        <span>{stateLabel}</span>
                      </label>
                    </span>
                  </td>
                  <td className="px-4 py-4">{module.roles.map((role) => role.name).join(", ") || "—"}</td>
                  <td className="px-4 py-4"><div className="flex flex-wrap gap-1">
                    <Button size="sm" variant="ghost" onClick={() => setEditingModule(module)}>{t("edit")}</Button>
                    <Link href="/dashboard/permissions" className="inline-flex h-7 items-center justify-center rounded-lg px-2.5 text-[0.8rem] font-medium transition-colors hover:bg-muted hover:text-foreground">{t("assignRoles")}</Link>
                  </div></td>
                </tr>;
              })}
            </tbody>
          </table>
        </div>
      )}
      {editingModule && <ModuleFormDialog module={editingModule} onClose={() => setEditingModule(null)} onSubmit={async (values) => {
        await updateMutation.mutateAsync({ module: editingModule, values });
        notify.success(t("saved"));
        setEditingModule(null);
      }} />}
    </div>
  );
}
