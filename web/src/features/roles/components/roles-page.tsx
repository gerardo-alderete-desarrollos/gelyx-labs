"use client";

import Link from "next/link";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Can } from "@/features/auth/components/can";
import { DeleteRoleDialog } from "@/features/roles/components/delete-role-dialog";
import { useRoles } from "@/features/roles/hooks/use-roles";
import { RoleFormDialog } from "@/features/roles/components/role-form-dialog";
import type { Role, RoleFormValues } from "@/features/roles/schemas/roles.schema";
import { rolesService } from "@/features/roles/services/roles.service";
import type { RoleDeleteVariables, RoleUpdateVariables } from "@/features/roles/types/roles.types";
import { useTranslations } from "next-intl";

export function RolesPage() {
  const t = useTranslations("roles");
  const common = useTranslations("common");
  const queryClient = useQueryClient();
  const rolesQuery = useRoles();
  const [editingRole, setEditingRole] = useState<Role | null>(null);
  const [creating, setCreating] = useState(false);
  const [deletingRole, setDeletingRole] = useState<Role | null>(null);
  const invalidateRoles = async () => {
    await queryClient.invalidateQueries({ queryKey: ["roles"] });
    await queryClient.invalidateQueries({ queryKey: ["permissions"] });
    await queryClient.invalidateQueries({ queryKey: ["users"] });
  };
  const createMutation = useMutation({ mutationFn: (values: RoleFormValues) => rolesService.create(values), onSuccess: invalidateRoles });
  const updateMutation = useMutation({
    mutationFn: ({ role, values }: RoleUpdateVariables) => rolesService.update(role.id, values),
    onSuccess: invalidateRoles,
  });
  const deleteMutation = useMutation({
    mutationFn: ({ role, targetRoleId }: RoleDeleteVariables) => rolesService.remove(role.id, targetRoleId),
    onSuccess: invalidateRoles,
  });
  const roles = rolesQuery.data ?? [];

  return (
    <div className="space-y-6">
      <header className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div><p className="text-sm font-medium text-primary">{common("administration")}</p><h1 className="mt-1 text-3xl font-semibold tracking-tight">{t("title")}</h1><p className="mt-2 text-muted-foreground">{t("description")}</p></div>
        <Can permission="roles.create"><Button onClick={() => setCreating(true)}>{t("create")}</Button></Can>
      </header>
      {rolesQuery.isPending ? <div className="h-64 animate-pulse rounded-2xl bg-muted" aria-label={t("loading")} /> : rolesQuery.isError ? (
        <section role="alert" className="rounded-2xl border border-destructive/30 bg-card p-8 text-center"><p className="text-destructive">{t("loadError")}</p><Button className="mt-4" variant="outline" onClick={() => void rolesQuery.refetch()}>{common("retry")}</Button></section>
      ) : roles.length === 0 ? (
        <section className="rounded-2xl border border-dashed border-border bg-card p-10 text-center"><h2 className="text-lg font-semibold">{t("empty")}</h2><Can permission="roles.create"><Button className="mt-4" onClick={() => setCreating(true)}>{t("create")}</Button></Can></section>
      ) : (
        <div className="overflow-x-auto rounded-2xl border border-border bg-card">
          <table className="w-full text-left text-sm">
            <thead className="bg-muted/60 text-muted-foreground"><tr>{[t("name"), t("descriptionColumn"), t("users"), t("permissions"), t("actions")].map((heading) => <th key={heading} scope="col" className="px-4 py-3 font-medium">{heading}</th>)}</tr></thead>
            <tbody className="divide-y divide-border">{roles.map((role) => <tr key={role.id}>
              <td className="px-4 py-4 font-medium">{role.name}{role.isSystem && <span className="ml-2 rounded-full bg-muted px-2 py-1 text-xs text-muted-foreground">{common("base")}</span>}</td>
              <td className="max-w-sm px-4 py-4 text-muted-foreground">{role.description || "—"}</td>
              <td className="px-4 py-4">{role.userCount}</td>
              <td className="px-4 py-4">{role.permissionCount}</td>
              <td className="px-4 py-4"><div className="flex flex-wrap gap-1">
                <Can permission="roles.update"><Button size="sm" variant="ghost" onClick={() => setEditingRole(role)}>{t("edit")}</Button></Can>
                <Can permission="permissions.read"><Link href={`/dashboard/permissions?roleId=${encodeURIComponent(role.id)}`} className="inline-flex h-7 items-center justify-center rounded-lg px-2.5 text-[0.8rem] font-medium transition-colors hover:bg-muted hover:text-foreground">{t("viewPermissions")}</Link></Can>
                {!role.isSystem && <Can permission="roles.delete"><Button size="sm" variant="destructive" onClick={() => setDeletingRole(role)}>{t("delete")}</Button></Can>}
              </div></td>
            </tr>)}</tbody>
          </table>
        </div>
      )}
      {creating && <RoleFormDialog roles={roles} onClose={() => setCreating(false)} onSubmit={async (values) => { await createMutation.mutateAsync(values); setCreating(false); }} />}
      {editingRole && <RoleFormDialog roles={roles} role={editingRole} onClose={() => setEditingRole(null)} onSubmit={async (values) => { await updateMutation.mutateAsync({ role: editingRole, values }); setEditingRole(null); }} />}
      {deletingRole && <DeleteRoleDialog role={deletingRole} roles={roles} onClose={() => setDeletingRole(null)} onConfirm={async (targetRoleId) => { await deleteMutation.mutateAsync({ role: deletingRole, targetRoleId }); setDeletingRole(null); }} />}
    </div>
  );
}
