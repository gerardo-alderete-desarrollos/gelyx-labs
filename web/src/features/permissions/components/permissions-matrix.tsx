"use client";

import { Fragment } from "react";
import { useMemo, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { notify } from "@/lib/notify";
import { Button } from "@/components/ui/button";
import { Can } from "@/features/auth/components/can";
import { useSession } from "@/features/auth/hooks/use-session";
import { getPermissionsMatrix, saveRolePermissions } from "@/features/permissions/services/permissions.service";
import type { PermissionsMatrixData } from "@/features/permissions/schemas/permissions.schema";
import { useTranslations } from "next-intl";

type DraftPermissions = Record<string, string[]>;

function initialDraft(matrix: PermissionsMatrixData): DraftPermissions {
  return Object.fromEntries(matrix.roles.map((role) => [
    role.id,
    role.modules.flatMap((module) => module.permissions.filter((permission) => permission.granted).map((permission) => permission.key)),
  ]));
}

function isGranted(draft: DraftPermissions, roleId: string, key: string) {
  return draft[roleId]?.includes(key) ?? false;
}

function togglePermission(draft: DraftPermissions, roleId: string, key: string, checked: boolean, moduleKey: string): DraftPermissions {
  const permissions = new Set(draft[roleId] ?? []);
  if (key === `${moduleKey}.access` && !checked) {
    for (const item of permissions) if (item.startsWith(`${moduleKey}.`)) permissions.delete(item);
  } else if (checked) {
    permissions.add(key);
    if (key !== `${moduleKey}.access`) permissions.add(`${moduleKey}.access`);
  } else {
    permissions.delete(key);
  }
  return { ...draft, [roleId]: [...permissions].sort() };
}

export function PermissionsMatrix({ focusedRoleId }: Readonly<{ focusedRoleId?: string }>) {
  const t = useTranslations("permissions");
  const common = useTranslations("common");
  const errors = useTranslations("errors");
  const matrixQuery = useQuery({ queryKey: ["permissions", "matrix"], queryFn: getPermissionsMatrix });
  const queryClient = useQueryClient();
  const { data: session } = useSession();
  const canManage = session?.permissions.includes("permissions.manage") ?? false;
  const [draftOverride, setDraftOverride] = useState<DraftPermissions | null>(null);
  const [expanded, setExpanded] = useState<string[]>([]);
  const [confirming, setConfirming] = useState(false);
  const [saving, setSaving] = useState(false);

  const matrix = matrixQuery.data;
  const draft = useMemo(
    () => draftOverride ?? (matrix ? initialDraft(matrix) : {}),
    [draftOverride, matrix],
  );
  const updateDraft = (update: (current: DraftPermissions) => DraftPermissions) => {
    if (matrix) setDraftOverride((current) => update(current ?? initialDraft(matrix)));
  };
  const dirty = useMemo(() => {
    if (!matrix) return false;
    const baseline = initialDraft(matrix);
    return matrix.roles.some((role) => {
      const saved = [...(baseline[role.id] ?? [])].sort();
      const changed = [...(draft[role.id] ?? [])].sort();
      return saved.length !== changed.length || saved.some((key, index) => key !== changed[index]);
    });
  }, [draft, matrix]);

  if (matrixQuery.isPending) return <div className="h-80 animate-pulse rounded-2xl bg-muted" aria-label={t("loading")} />;
  if (matrixQuery.isError || !matrix) {
    const code = matrixQuery.error instanceof Error ? matrixQuery.error.message : "generic";
    return <section role="alert" className="rounded-2xl border border-destructive/30 p-6 text-destructive">{errors.has(code) ? errors(code) : errors("generic")} <Button variant="outline" onClick={() => matrixQuery.refetch()}>{common("retry")}</Button></section>;
  }

  const roleDiff = matrix.roles.flatMap((role) => {
    const initial = initialDraft(matrix)[role.id] ?? [];
    const current = draft[role.id] ?? [];
    const granted = current.filter((key) => !initial.includes(key));
    const removed = initial.filter((key) => !current.includes(key));
    return granted.length || removed.length ? [{ role: role.name, granted, removed }] : [];
  });
  const systemModuleKeys = matrix.modules.filter((module) => module.isSystem).map((module) => module.key);

  const save = async () => {
    setSaving(true);
    try {
      for (const change of roleDiff) {
        const role = matrix.roles.find((item) => item.name === change.role);
        if (role) await saveRolePermissions(role.id, draft[role.id] ?? []);
      }
      setDraftOverride(null);
      await queryClient.invalidateQueries({ queryKey: ["permissions"] });
      await queryClient.invalidateQueries({ queryKey: ["session"] });
      notify.success(t("saved"));
      setConfirming(false);
    } catch (error) {
      const code = error instanceof Error ? error.message : "";
      notify.error(errors.has(code) ? errors(code) : errors("generic"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="space-y-5">
      <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-end"><div><p className="text-sm font-medium text-primary">{common("administration")}</p><h1 className="mt-1 text-3xl font-semibold tracking-tight">{t("title")}</h1><p className="mt-2 text-muted-foreground">{t("description")}</p></div><p className="max-w-xl text-sm text-muted-foreground">{t("immediateChanges")}</p></div>
      <div className="relative">
        <div id="permissions-matrix-scroll" data-testid="permissions-matrix-scroll" className="overflow-x-auto overflow-y-auto rounded-2xl border border-border bg-card max-h-[calc(100vh-280px)]">
          <table className="min-w-full border-separate border-spacing-0 text-sm">
            <thead>
              <tr>
                <th className="sticky top-0 sticky left-0 z-[var(--z-sticky-corner)] min-w-64 border-b border-r border-border bg-card px-4 py-3 text-left shadow-sm">
                  {t("modulePermission")}
                </th>
                {matrix.roles.map((role) => (
                  <th key={role.id} className={`sticky top-0 z-[var(--z-sticky-head)] min-w-36 border-b border-l border-border px-4 py-3 text-center ${focusedRoleId === role.id ? "bg-primary/10" : "bg-muted"}`}>
                    <span className="block font-semibold">{role.name}</span>
                    {role.name === "Admin" && <span className="text-xs text-muted-foreground">{t("alwaysComplete")}</span>}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {matrix.modules.map((module) => {
                const isExpanded = expanded.includes(module.key);
                return (
                  <Fragment key={module.key}>
                    <tr>
                      <th scope="row" className="sticky left-0 z-[var(--z-sticky-col)] border-b border-r border-border bg-card px-4 py-3 text-left shadow-sm">
                        <button className="font-semibold hover:text-primary" aria-expanded={isExpanded} onClick={() => setExpanded((current) => isExpanded ? current.filter((key) => key !== module.key) : [...current, module.key])}>
                          {isExpanded ? "−" : "+"} {module.name}
                        </button>
                        {module.isSystem && <span className="ml-2 text-xs text-muted-foreground" title={t("systemOnlyAdmin")}>🔒 {t("system")}</span>}
                      </th>
                      {matrix.roles.map((role) => {
                        const lockedAdmin = role.name === "Admin";
                        const lockedSystem = module.isSystem && !lockedAdmin;
                        const accessKey = `${module.key}.access`;
                        return (
                          <td key={role.id} className={`border-b border-l border-border px-4 py-3 text-center ${focusedRoleId === role.id ? "bg-primary/5" : ""}`}>
                            <input
                              aria-label={`${module.name} — ${role.name}`}
                              type="checkbox"
                              checked={lockedAdmin || isGranted(draft, role.id, accessKey)}
                              disabled={lockedAdmin || lockedSystem || !canManage}
                              title={lockedSystem ? t("systemOnlyAdmin") : lockedAdmin ? t("adminAlwaysAll") : !canManage ? t("noEditPermission") : undefined}
                              onChange={(event) => updateDraft((current) => togglePermission(current, role.id, accessKey, event.target.checked, module.key))}
                            />
                          </td>
                        );
                      })}
                    </tr>
                    {isExpanded && module.permissions.filter((permission) => permission.key !== `${module.key}.access`).map((permission) => (
                      <tr key={`${module.key}:${permission.key}`}>
                        <th scope="row" className="sticky left-0 z-[var(--z-sticky-col)] border-b border-r border-border bg-card px-8 py-3 text-left font-normal text-muted-foreground shadow-sm">
                          {permission.name}
                        </th>
                        {matrix.roles.map((role) => {
                          const lockedAdmin = role.name === "Admin";
                          const lockedSystem = systemModuleKeys.includes(module.key) && !lockedAdmin;
                          return (
                            <td key={role.id} className={`border-b border-l border-border px-4 py-3 text-center ${focusedRoleId === role.id ? "bg-primary/5" : ""}`}>
                              <input
                                aria-label={`${permission.name} — ${role.name}`}
                                type="checkbox"
                                checked={lockedAdmin || isGranted(draft, role.id, permission.key)}
                                disabled={lockedAdmin || lockedSystem || !canManage}
                                title={lockedSystem ? t("systemOnlyAdmin") : !canManage ? t("noEditPermission") : undefined}
                                onChange={(event) => updateDraft((current) => togglePermission(current, role.id, permission.key, event.target.checked, module.key))}
                              />
                            </td>
                          );
                        })}
                      </tr>
                    ))}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
        {dirty && (
          <div id="permissions-changes-bar" data-testid="permissions-changes-bar" className="sticky bottom-0 z-[var(--z-changes-bar)] flex flex-col gap-3 rounded-t-2xl border-t border-primary/20 bg-card px-4 py-4 shadow-xl sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm">{t("unsaved", { count: roleDiff.length })}</p>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setDraftOverride(initialDraft(matrix))}>{t("discard")}</Button>
              <Can permission="permissions.manage">
                <Button onClick={() => setConfirming(true)}>{t("save")}</Button>
              </Can>
            </div>
          </div>
        )}
      </div>
      {confirming && (
        <div className="fixed inset-0 z-[var(--z-dialog)] grid place-items-center bg-black/50 p-4">
          <section role="alertdialog" aria-modal="true" aria-labelledby="permissions-confirm-title" className="w-full max-w-xl rounded-2xl border border-border bg-card p-6 shadow-xl">
            <h2 id="permissions-confirm-title" className="text-lg font-semibold">{t("confirmTitle")}</h2>
            <div className="mt-4 max-h-72 space-y-4 overflow-auto text-sm">
              {roleDiff.map((change) => (
                <div key={change.role}>
                  <h3 className="font-semibold">{change.role}</h3>
                  {change.granted.length > 0 && <p className="mt-1 text-emerald-700">{t("granted", { permissions: change.granted.join(", ") })}</p>}
                  {change.removed.length > 0 && <p className="mt-1 text-destructive">{t("removed", { permissions: change.removed.join(", ") })}</p>}
                </div>
              ))}
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <Button variant="outline" disabled={saving} onClick={() => setConfirming(false)}>{common("cancel")}</Button>
              <Button disabled={saving} onClick={() => void save()}>{saving ? common("saving") : t("confirmSave")}</Button>
            </div>
          </section>
        </div>
      )}
    </div>
  );
}