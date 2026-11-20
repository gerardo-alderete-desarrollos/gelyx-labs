"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import type { Role } from "@/features/roles/schemas/roles.schema";
import { useTranslations } from "next-intl";

export function DeleteRoleDialog({
  role,
  roles,
  onClose,
  onConfirm,
}: Readonly<{
  role: Role;
  roles: Role[];
  onClose: () => void;
  onConfirm: (reassignToRoleId?: string) => Promise<void>;
}>) {
  const t = useTranslations("roles");
  const errors = useTranslations("errors");
  const common = useTranslations("common");
  const [targetRoleId, setTargetRoleId] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const requiresReassignment = role.userCount > 0;

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <section role="alertdialog" aria-modal="true" aria-labelledby="delete-role-title" className="w-full max-w-md rounded-2xl border border-border bg-card p-6 shadow-xl">
        <h2 id="delete-role-title" className="text-lg font-semibold">{t("deleteTitle")}</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          {requiresReassignment
            ? t("deleteWithUsers", { name: role.name, count: role.userCount })
            : t("deleteWithoutUsers", { name: role.name })}
        </p>
        {requiresReassignment && <label className="mt-4 block space-y-1.5 text-sm">
          <span>{t("reassign")}</span>
          <select aria-label={t("reassign")} value={targetRoleId} onChange={(event) => setTargetRoleId(event.target.value)} className="h-10 w-full rounded-lg border border-input bg-background px-3">
            <option value="">{t("selectRole")}</option>
            {roles.filter((item) => item.id !== role.id).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
          </select>
        </label>}
        {error && <p role="alert" className="mt-3 text-sm text-destructive">{error}</p>}
        <div className="mt-6 flex justify-end gap-2">
          <Button variant="outline" disabled={busy} onClick={onClose}>{common("cancel")}</Button>
          <Button variant="destructive" disabled={busy || (requiresReassignment && !targetRoleId)} onClick={async () => {
            setBusy(true);
            setError("");
            try {
              await onConfirm(targetRoleId || undefined);
              onClose();
            } catch (caught) {
              const code = caught instanceof Error ? caught.message : "generic";
              setError(errors.has(code) ? errors(code) : errors("generic"));
            } finally {
              setBusy(false);
            }
          }}>{busy ? t("deleting") : t("delete")}</Button>
        </div>
      </section>
    </div>
  );
}
