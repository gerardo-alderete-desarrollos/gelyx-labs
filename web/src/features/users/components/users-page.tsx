"use client";

import { useEffect, useMemo, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import { Can } from "@/features/auth/components/can";
import {
  userEditSchema,
  type RoleOption,
  type UserEditValues,
  type UserFormValues,
  type UserListItem,
} from "@/features/users/schemas/users.schema";
import { ApiRequestError, usersService } from "@/features/users/services/users.service";
import { UserFormDialog } from "@/features/users/components/user-form-dialog";
import { useSession } from "@/features/auth/hooks/use-session";
import { notify } from "@/lib/notify";
import { ExportCsvButton } from "@/components/export-csv-button";

type Confirmation = {
  title: string;
  description: string;
  confirmLabel: string;
  run: () => Promise<void>;
} | null;

const queryKey = ["users"];
const date = (value: string | null, locale: string, noRecord: string) => value
  ? new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value))
  : noRecord;

function errorMessage(
  error: unknown,
  users: (key: "concurrencyConflict" | "actionError") => string,
  apiErrors: (key: string) => string,
  hasApiError: (key: string) => boolean,
) {
  if (error instanceof ApiRequestError) {
    return hasApiError(error.code) ? apiErrors(error.code) : apiErrors("generic");
  }
  return users("actionError");
}

function ConfirmDialog({ confirmation, onClose }: Readonly<{ confirmation: Confirmation; onClose: () => void }>) {
  const t = useTranslations("users");
  const errors = useTranslations("errors");
  const common = useTranslations("common");
  const [busy, setBusy] = useState(false);
  if (!confirmation) return null;
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <section role="alertdialog" aria-modal="true" aria-labelledby="confirm-title" className="w-full max-w-md rounded-2xl border border-border bg-card p-6 shadow-xl">
        <h2 id="confirm-title" className="text-lg font-semibold">{confirmation.title}</h2>
        <p className="mt-2 text-sm text-muted-foreground">{confirmation.description}</p>
        <div className="mt-6 flex justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={busy}>{common("cancel")}</Button>
          <Button variant="destructive" disabled={busy} onClick={async () => {
            setBusy(true);
            try {
              await confirmation.run();
              onClose();
            } catch (error) {
              notify.error(errorMessage(error, t, errors, errors.has));
            } finally {
              setBusy(false);
            }
          }}>{busy ? t("processing") : confirmation.confirmLabel}</Button>
        </div>
      </section>
    </div>
  );
}

function RoleBadge({ name }: Readonly<{ name: string }>) {
  const hue = [...name].reduce((hash, character) => (hash * 31 + character.charCodeAt(0)) >>> 0, 0) % 360;
  return <span className="inline-flex rounded-full px-2.5 py-1 text-xs font-medium" style={{ backgroundColor: `hsl(${hue} 72% 92%)`, color: `hsl(${hue} 60% 28%)` }}>{name}</span>;
}

function UserEditDialog({
  user,
  onClose,
  onSubmit,
}: Readonly<{
  user: UserListItem;
  onClose: () => void;
  onSubmit: (values: UserEditValues) => Promise<void>;
}>) {
  const t = useTranslations("users");
  const common = useTranslations("common");
  const validation = useTranslations("auth.validation");
  const errors = useTranslations("errors");
  const [values, setValues] = useState({
    email: user.email,
    fullName: user.fullName ?? "",
    phoneNumber: user.phoneNumber ?? "",
  });
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <section role="dialog" aria-modal="true" aria-labelledby="edit-user-title" className="w-full max-w-lg rounded-2xl border border-border bg-card p-6 shadow-xl">
        <h2 id="edit-user-title" className="text-xl font-semibold">{t("editTitle")}</h2>
        <form className="mt-5 space-y-4" onSubmit={async (event) => {
          event.preventDefault();
          const parsed = userEditSchema.safeParse({ ...values, phoneNumber: values.phoneNumber || null });
          if (!parsed.success) {
            setFieldErrors(Object.fromEntries(parsed.error.issues.map((issue) => [String(issue.path[0]), validation(issue.message)])));
            return;
          }
          setError("");
          setFieldErrors({});
          setBusy(true);
          try {
            await onSubmit(parsed.data);
          } catch (caught) {
            if (caught instanceof ApiRequestError && Object.keys(caught.fieldErrors).length > 0) {
              setFieldErrors(Object.fromEntries(Object.entries(caught.fieldErrors).map(([key, messages]) => [key, messages.join(" ")])));
            } else {
              setError(errorMessage(caught, t, errors, errors.has));
            }
          } finally { setBusy(false); }
        }}>
          <label className="block space-y-1 text-sm"><span>{t("fullName")}</span><input className="h-10 w-full rounded-lg border border-input bg-background px-3" value={values.fullName} onChange={(event) => setValues({ ...values, fullName: event.target.value })} />{fieldErrors.fullName && <span role="alert" className="text-destructive">{fieldErrors.fullName}</span>}</label>
          <label className="block space-y-1 text-sm"><span>{t("email")}</span><input type="email" className="h-10 w-full rounded-lg border border-input bg-background px-3" value={values.email} onChange={(event) => setValues({ ...values, email: event.target.value })} />{fieldErrors.email && <span role="alert" className="text-destructive">{fieldErrors.email}</span>}</label>
          <label className="block space-y-1 text-sm"><span>{t("phone")}</span><input className="h-10 w-full rounded-lg border border-input bg-background px-3" value={values.phoneNumber} onChange={(event) => setValues({ ...values, phoneNumber: event.target.value })} />{fieldErrors.phoneNumber && <span role="alert" className="text-destructive">{fieldErrors.phoneNumber}</span>}</label>
          {error && <p role="alert" className="text-sm text-destructive">{error}</p>}
          <div className="flex justify-end gap-2"><Button type="button" variant="outline" onClick={onClose}>{common("cancel")}</Button><Button type="submit" disabled={busy}>{busy ? common("saving") : common("save")}</Button></div>
        </form>
      </section>
    </div>
  );
}

function UserDetailsPanel({ userId, locale, onClose }: Readonly<{ userId: string; locale: string; onClose: () => void }>) {
  const t = useTranslations("users");
  const errors = useTranslations("errors");
  const details = useQuery({ queryKey: ["user", userId], queryFn: () => usersService.details(userId) });
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <section role="dialog" aria-modal="true" aria-labelledby="user-details-title" className="w-full max-w-lg rounded-2xl border border-border bg-card p-6 shadow-xl">
        <div className="flex items-start justify-between"><h2 id="user-details-title" className="text-xl font-semibold">{t("detailTitle")}</h2><Button variant="ghost" aria-label={t("closeDetails")} onClick={onClose}>×</Button></div>
        {details.isPending ? <p className="py-8 text-muted-foreground">{t("loadingUser")}</p> : details.isError ? <p role="alert" className="py-8 text-destructive">{errorMessage(details.error, t, errors, errors.has)}</p> : (
          <dl className="mt-5 grid gap-4 text-sm sm:grid-cols-2">
            <div><dt className="text-muted-foreground">{t("name")}</dt><dd className="mt-1 font-medium">{details.data.fullName}</dd></div>
            <div><dt className="text-muted-foreground">{t("emailShort")}</dt><dd className="mt-1 font-medium">{details.data.email}</dd></div>
            <div><dt className="text-muted-foreground">{t("phone")}</dt><dd className="mt-1 font-medium">{details.data.phoneNumber || "—"}</dd></div>
            <div><dt className="text-muted-foreground">{t("lastLogin")}</dt><dd className="mt-1 font-medium">{date(details.data.lastLoginAt, locale, t("noRecord"))}</dd></div>
            <div><dt className="text-muted-foreground">{t("pendingPasswordChange")}</dt><dd className="mt-1 font-medium">{details.data.mustChangePassword ? t("yes") : t("no")}</dd></div>
            <div><dt className="text-muted-foreground">{t("roles")}</dt><dd className="mt-1 flex flex-wrap gap-1">{details.data.roles.map((role) => <RoleBadge key={role.id} name={role.name} />)}</dd></div>
          </dl>
        )}
      </section>
    </div>
  );
}

function AssignRolesDialog({
  user,
  roles,
  activeAdminCount,
  adminsLoading,
  onClose,
  onSubmit,
}: Readonly<{
  user: UserListItem;
  roles: RoleOption[];
  activeAdminCount?: number;
  adminsLoading: boolean;
  onClose: () => void;
  onSubmit: (roleIds: string[]) => Promise<void>;
}>) {
  const t = useTranslations("users");
  const common = useTranslations("common");
  const errors = useTranslations("errors");
  const [selectedOverride, setSelectedOverride] = useState<string[] | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const details = useQuery({ queryKey: ["user", user.id], queryFn: () => usersService.details(user.id) });
  const selected = selectedOverride ?? details.data?.roles.map((role) => role.id) ?? [];
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-black/50 p-4">
      <section role="dialog" aria-modal="true" aria-labelledby="assign-roles-title" className="w-full max-w-lg rounded-2xl border border-border bg-card p-6 shadow-xl">
        <h2 id="assign-roles-title" className="text-xl font-semibold">{t("assignRoles")}</h2>
        {details.isPending ? <p className="py-6 text-muted-foreground">{t("loadingRoles")}</p> : (
          <div className="mt-4 grid gap-2 sm:grid-cols-2">
            {roles.map((role) => {
              const protectsLastAdmin = role.name === "Admin" && user.isActive && user.roles.includes("Admin") && (adminsLoading || activeAdminCount === 1);
              return <label key={role.id} className="flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm"><input type="checkbox" checked={selected.includes(role.id)} disabled={protectsLastAdmin} title={protectsLastAdmin ? t("cannotRemoveLastAdmin") : undefined} onChange={(event) => setSelectedOverride(event.target.checked ? [...selected, role.id] : selected.filter((id) => id !== role.id))} />{role.name}</label>;
            })}
          </div>
        )}
        {error && <p role="alert" className="mt-3 text-sm text-destructive">{error}</p>}
        <div className="mt-6 flex justify-end gap-2"><Button variant="outline" onClick={onClose}>{common("cancel")}</Button><Button disabled={busy || details.isPending} onClick={async () => {
          setBusy(true);
          try { await onSubmit(selected); } catch (caught) { setError(errorMessage(caught, t, errors, errors.has)); } finally { setBusy(false); }
        }}>{busy ? common("saving") : t("saveRoles")}</Button></div>
      </section>
    </div>
  );
}

export function UsersPage({ initialQuery }: Readonly<{ initialQuery: Record<string, string | undefined> }>) {
  const t = useTranslations("users");
  const common = useTranslations("common");
  const errors = useTranslations("errors");
  const locale = useLocale();
  const router = useRouter();
  const pathname = usePathname();
  const queryClient = useQueryClient();
  const { data: session } = useSession();
  const [searchInput, setSearchInput] = useState(initialQuery.search ?? "");
  const [filters, setFilters] = useState({
    roleId: initialQuery.roleId ?? "",
    status: initialQuery.status ?? "",
    sortBy: initialQuery.sortBy ?? "name",
    sortDir: initialQuery.sortDir ?? "asc",
  });
  const page = Math.max(1, Number(initialQuery.page) || 1);
  const pageSize = Math.min(100, Math.max(1, Number(initialQuery.pageSize) || 20));
  const search = initialQuery.search ?? "";
  const queryParams = useMemo(() => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize), sortBy: filters.sortBy, sortDir: filters.sortDir });
    if (search) params.set("search", search);
    if (filters.roleId) params.set("roleId", filters.roleId);
    if (filters.status) params.set("status", filters.status);
    return params;
  }, [filters, page, pageSize, search]);
  const usersQuery = useQuery({ queryKey: [...queryKey, queryParams.toString()], queryFn: () => usersService.list(queryParams) });
  const rolesQuery = useQuery({ queryKey: ["roles"], queryFn: usersService.roles });
  const adminRoleId = rolesQuery.data?.find((role) => role.name === "Admin")?.id;
  const activeAdminsQuery = useQuery({
    queryKey: [...queryKey, "active-admins", adminRoleId],
    queryFn: () => usersService.list(new URLSearchParams({ page: "1", pageSize: "1", roleId: adminRoleId!, status: "active" })),
    enabled: Boolean(adminRoleId),
  });
  const [createOpen, setCreateOpen] = useState(false);
  const [editUser, setEditUser] = useState<UserListItem | null>(null);
  const [detailsUserId, setDetailsUserId] = useState<string | null>(null);
  const [rolesUser, setRolesUser] = useState<UserListItem | null>(null);
  const [confirmation, setConfirmation] = useState<Confirmation>(null);
  const [temporaryPassword, setTemporaryPassword] = useState("");

  const refreshUsers = () => queryClient.invalidateQueries({ queryKey });
  const createMutation = useMutation({
    mutationFn: (values: UserFormValues) => usersService.create(values),
    onSuccess: async (created) => {
      setCreateOpen(false);
      setTemporaryPassword(created.temporaryPassword);
      await refreshUsers();
      notify.success(t("created"));
    },
    onError: (error) => notify.error(errorMessage(error, t, errors, errors.has)),
  });
  const updateMutation = useMutation({
    mutationFn: ({ user, values }: { user: UserListItem; values: UserEditValues }) => usersService.update(user.id, values, user.rowVersion),
    onSuccess: async () => { setEditUser(null); await refreshUsers(); notify.success(t("updated")); },
    onError: (error) => notify.error(errorMessage(error, t, errors, errors.has)),
  });
  const statusMutation = useMutation({
    mutationFn: ({ user, isActive }: { user: UserListItem; isActive: boolean }) => usersService.setStatus(user.id, isActive),
    onMutate: async ({ user, isActive }) => {
      await queryClient.cancelQueries({ queryKey: [...queryKey, queryParams.toString()] });
      const key = [...queryKey, queryParams.toString()];
      const previous = queryClient.getQueryData<typeof usersQuery.data>(key);
      queryClient.setQueryData(key, (data: typeof usersQuery.data) => data && ({
        ...data,
        items: data.items.map((item) => item.id === user.id ? { ...item, isActive } : item),
      }));
      return { previous, key };
    },
    onError: (error, _input, context) => {
      if (context?.previous) queryClient.setQueryData(context.key, context.previous);
      notify.error(errorMessage(error, t, errors, errors.has));
    },
    onSettled: refreshUsers,
    onSuccess: () => notify.success(t("statusUpdated")),
  });

  useEffect(() => {
    const timeout = window.setTimeout(() => {
      if (searchInput !== search) {
        const params = new URLSearchParams(queryParams);
        params.set("page", "1");
        if (searchInput.trim()) params.set("search", searchInput.trim());
        else params.delete("search");
        router.replace(`${pathname}?${params.toString()}`);
      }
    }, 350);
    return () => window.clearTimeout(timeout);
  }, [searchInput, search, queryParams, pathname, router]);

  const updateFilter = (key: keyof typeof filters, value: string) => {
    const next = { ...filters, [key]: value };
    setFilters(next);
    const params = new URLSearchParams(queryParams);
    params.set("page", "1");
    if (value) params.set(key, value);
    else params.delete(key);
    router.replace(`${pathname}?${params.toString()}`);
  };
  const updateSort = (value: string) => {
    const [sortBy, sortDir] = value.split(":");
    const next = { ...filters, sortBy, sortDir };
    setFilters(next);
    const params = new URLSearchParams(queryParams);
    params.set("page", "1");
    params.set("sortBy", sortBy);
    params.set("sortDir", sortDir);
    router.replace(`${pathname}?${params.toString()}`);
  };
  const movePage = (nextPage: number) => {
    const params = new URLSearchParams(queryParams);
    params.set("page", String(nextPage));
    router.replace(`${pathname}?${params.toString()}`);
  };
  const roles = rolesQuery.data ?? [];
  const rows = usersQuery.data?.items ?? [];

  return (
    <div className="space-y-6">
      <header className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div><p className="text-sm font-medium text-primary">{common("administration")}</p><h1 className="mt-1 text-3xl font-semibold tracking-tight">{t("title")}</h1><p className="mt-2 text-muted-foreground">{t("description")}</p></div>
        <div className="flex gap-2">
          <Can permission="users.read"><ExportCsvButton path={`/api/users/export?${queryParams}`} label={t("export")} loadingLabel={t("exporting")} errorMessage={t("exportError")} fallbackFileName="users.csv" /></Can>
          <Can permission="users.create"><Button onClick={() => setCreateOpen(true)}>{t("create")}</Button></Can>
        </div>
      </header>
      <section className="grid gap-3 rounded-2xl border border-border bg-card p-4 sm:grid-cols-2 lg:grid-cols-4">
        <label className="space-y-1 text-sm"><span>{t("search")}</span><input aria-label={t("search")} className="h-10 w-full rounded-lg border border-input bg-background px-3" placeholder={t("searchPlaceholder")} value={searchInput} onChange={(event) => setSearchInput(event.target.value)} /></label>
        <label className="space-y-1 text-sm"><span>{t("role")}</span><select aria-label={t("filterRole")} className="h-10 w-full rounded-lg border border-input bg-background px-3" value={filters.roleId} onChange={(event) => updateFilter("roleId", event.target.value)}><option value="">{t("allRoles")}</option>{roles.map((role) => <option key={role.id} value={role.id}>{role.name}</option>)}</select></label>
        <label className="space-y-1 text-sm"><span>{t("status")}</span><select aria-label={t("filterStatus")} className="h-10 w-full rounded-lg border border-input bg-background px-3" value={filters.status} onChange={(event) => updateFilter("status", event.target.value)}><option value="">{t("notDeleted")}</option><option value="active">{t("active")}</option><option value="inactive">{t("inactive")}</option><option value="deleted">{t("deleted")}</option></select></label>
        <label className="space-y-1 text-sm"><span>{t("sort")}</span><select aria-label={t("sort")} className="h-10 w-full rounded-lg border border-input bg-background px-3" value={`${filters.sortBy}:${filters.sortDir}`} onChange={(event) => {
          updateSort(event.target.value);
        }}><option value="name:asc">{t("sortNameAsc")}</option><option value="name:desc">{t("sortNameDesc")}</option><option value="email:asc">{t("sortEmailAsc")}</option><option value="email:desc">{t("sortEmailDesc")}</option><option value="lastLoginAt:desc">{t("sortRecent")}</option><option value="lastLoginAt:asc">{t("sortOldest")}</option></select></label>
      </section>
      {rolesQuery.isError && <p role="alert" className="rounded-xl border border-destructive/30 p-4 text-destructive">{t("rolesLoadError")}</p>}
      {usersQuery.isPending ? <div className="h-64 animate-pulse rounded-2xl bg-muted" aria-label={t("loading")} /> : usersQuery.isError ? (
        <section role="alert" className="rounded-2xl border border-destructive/30 bg-card p-8 text-center"><p className="text-destructive">{errorMessage(usersQuery.error, t, errors, errors.has)}</p><Button className="mt-4" variant="outline" onClick={() => usersQuery.refetch()}>{common("retry")}</Button></section>
      ) : rows.length === 0 ? (
        <section className="rounded-2xl border border-dashed border-border bg-card p-10 text-center"><h2 className="text-lg font-semibold">{t("empty")}</h2><p className="mt-2 text-sm text-muted-foreground">{t("emptyDescription")}</p><Can permission="users.create"><Button className="mt-4" onClick={() => setCreateOpen(true)}>{t("create")}</Button></Can></section>
      ) : (
        <>
          <div className="hidden overflow-x-auto rounded-2xl border border-border bg-card md:block">
            <table className="w-full text-left text-sm"><thead className="bg-muted/60 text-muted-foreground"><tr>{[t("user"), t("roles"), t("status"), t("lastLogin"), t("actions")].map((heading) => <th key={heading} scope="col" className="px-4 py-3 font-medium">{heading}</th>)}</tr></thead>
              <tbody className="divide-y divide-border">{rows.map((user) => <tr key={user.id}>
                <td className="px-4 py-4"><p className="font-medium">{user.fullName}</p><p className="text-muted-foreground">{user.email}</p></td>
                <td className="px-4 py-4"><div className="flex flex-wrap gap-1">{user.roles.map((role) => <RoleBadge key={role} name={role} />)}</div></td>
                <td className="px-4 py-4"><span className={`rounded-full px-2.5 py-1 text-xs ${user.isDeleted ? "bg-muted text-muted-foreground" : user.isActive ? "bg-emerald-500/10 text-emerald-700" : "bg-amber-500/10 text-amber-700"}`}>{user.isDeleted ? t("deleted") : user.isActive ? t("active") : t("inactive")}</span></td>
                <td className="px-4 py-4 text-muted-foreground">{date(user.lastLoginAt, locale, t("noRecord"))}</td>
                <td className="px-4 py-4"><UserActions user={user} currentUserId={session?.id} activeAdminCount={activeAdminsQuery.data?.totalCount} adminsLoading={activeAdminsQuery.isPending} onDetails={() => setDetailsUserId(user.id)} onEdit={() => setEditUser(user)} onRoles={() => setRolesUser(user)} onStatus={async (isActive) => { await statusMutation.mutateAsync({ user, isActive }); }} onConfirm={setConfirmation} onTemporaryPassword={setTemporaryPassword} onRefresh={refreshUsers} /></td>
              </tr>)}</tbody>
            </table>
          </div>
          <div className="space-y-3 md:hidden">{rows.map((user) => <article key={user.id} className="space-y-4 rounded-2xl border border-border bg-card p-4">
            <div className="flex items-start justify-between gap-3"><div><h2 className="font-semibold">{user.fullName}</h2><p className="text-sm text-muted-foreground">{user.email}</p></div><span className="rounded-full bg-muted px-2.5 py-1 text-xs">{user.isDeleted ? t("deleted") : user.isActive ? t("active") : t("inactive")}</span></div>
            <div className="flex flex-wrap gap-1">{user.roles.map((role) => <RoleBadge key={role} name={role} />)}</div>
            <p className="text-xs text-muted-foreground">{t("lastLogin")}: {date(user.lastLoginAt, locale, t("noRecord"))}</p>
            <UserActions user={user} currentUserId={session?.id} activeAdminCount={activeAdminsQuery.data?.totalCount} adminsLoading={activeAdminsQuery.isPending} onDetails={() => setDetailsUserId(user.id)} onEdit={() => setEditUser(user)} onRoles={() => setRolesUser(user)} onStatus={async (isActive) => { await statusMutation.mutateAsync({ user, isActive }); }} onConfirm={setConfirmation} onTemporaryPassword={setTemporaryPassword} onRefresh={refreshUsers} />
          </article>)}</div>
          <div className="flex flex-col justify-between gap-3 text-sm text-muted-foreground sm:flex-row sm:items-center"><span>{t("count", { count: new Intl.NumberFormat(locale).format(usersQuery.data.totalCount), page: usersQuery.data.page, total: Math.max(1, usersQuery.data.totalPages) })}</span><div className="flex gap-2"><Button variant="outline" disabled={page <= 1} onClick={() => movePage(page - 1)}>{common("previous")}</Button><Button variant="outline" disabled={page >= usersQuery.data.totalPages} onClick={() => movePage(page + 1)}>{common("next")}</Button></div></div>
        </>
      )}
      {createOpen && <UserFormDialog roles={roles} onClose={() => setCreateOpen(false)} onSubmit={async (values) => { await createMutation.mutateAsync(values); }} />}
      {editUser && <UserEditDialog user={editUser} onClose={() => setEditUser(null)} onSubmit={async (values) => { await updateMutation.mutateAsync({ user: editUser, values }); }} />}
      {detailsUserId && <UserDetailsPanel userId={detailsUserId} locale={locale} onClose={() => setDetailsUserId(null)} />}
      {rolesUser && <AssignRolesDialog user={rolesUser} roles={roles} activeAdminCount={activeAdminsQuery.data?.totalCount} adminsLoading={activeAdminsQuery.isPending} onClose={() => setRolesUser(null)} onSubmit={async (roleIds) => { await usersService.assignRoles(rolesUser.id, roleIds); await refreshUsers(); notify.success(t("rolesUpdated")); setRolesUser(null); }} />}
      {temporaryPassword && <div className="fixed inset-0 z-[60] grid place-items-center bg-black/50 p-4"><section role="dialog" aria-modal="true" aria-labelledby="temporary-password-title" className="w-full max-w-md rounded-2xl border border-border bg-card p-6 shadow-xl"><h2 id="temporary-password-title" className="text-lg font-semibold">{t("temporaryPassword")}</h2><p className="mt-2 text-sm text-muted-foreground">{t("temporaryPasswordDescription")}</p><code className="mt-4 block break-all rounded-lg bg-muted p-3">{temporaryPassword}</code><div className="mt-5 flex justify-end gap-2"><Button variant="outline" onClick={() => void navigator.clipboard.writeText(temporaryPassword).then(() => notify.success(t("copied")))}>{t("copy")}</Button><Button onClick={() => setTemporaryPassword("")}>{t("done")}</Button></div></section></div>}
      <ConfirmDialog confirmation={confirmation} onClose={() => setConfirmation(null)} />
    </div>
  );
}

function UserActions({
  user,
  currentUserId,
  activeAdminCount,
  adminsLoading,
  onDetails,
  onEdit,
  onRoles,
  onStatus,
  onConfirm,
  onTemporaryPassword,
  onRefresh,
}: Readonly<{
  user: UserListItem;
  currentUserId?: string;
  activeAdminCount?: number;
  adminsLoading: boolean;
  onDetails: () => void;
  onEdit: () => void;
  onRoles: () => void;
  onStatus: (isActive: boolean) => Promise<void>;
  onConfirm: (confirmation: Confirmation) => void;
  onTemporaryPassword: (password: string) => void;
  onRefresh: () => Promise<void>;
}>) {
  const t = useTranslations("users");
  const self = user.id === currentUserId;
  const blocksLastAdmin = user.roles.includes("Admin") && user.isActive && (adminsLoading || activeAdminCount === 1);
  return (
    <div className="flex flex-wrap gap-1">
      <Can permission="users.read"><Button size="sm" variant="ghost" onClick={onDetails}>{t("details")}</Button></Can>
      <Can permission="users.update"><Button size="sm" variant="ghost" disabled={user.isDeleted} onClick={onEdit}>{t("edit")}</Button></Can>
      {!user.isDeleted && <Can permission="users.assign-roles"><Button size="sm" variant="ghost" onClick={onRoles}>{t("roles")}</Button></Can>}
      {!user.isDeleted && <Can permission="users.manage-status"><Button size="sm" variant="ghost" disabled={self || (blocksLastAdmin && user.isActive)} title={self ? t("noSelfDeactivate") : blocksLastAdmin ? t("lastAdminDeactivate") : undefined} onClick={() => onConfirm({ title: user.isActive ? t("deactivate") : t("activate"), description: user.isActive ? t("deactivateDescription") : t("activateDescription"), confirmLabel: user.isActive ? t("deactivate") : t("activate"), run: () => onStatus(!user.isActive) })}>{user.isActive ? t("deactivate") : t("activate")}</Button></Can>}
      {!user.isDeleted && <Can permission="users.reset-password"><Button size="sm" variant="ghost" onClick={() => onConfirm({ title: t("resetTitle"), description: t("resetDescription"), confirmLabel: t("resetConfirm"), run: async () => { const result = await usersService.resetPassword(user.id); await onRefresh(); onTemporaryPassword(result.temporaryPassword); } })}>{t("resetPassword")}</Button></Can>}
      {!user.isDeleted && <Can permission="users.delete"><Button size="sm" variant="destructive" disabled={self || blocksLastAdmin} title={self ? t("cannotDeleteSelf") : blocksLastAdmin ? t("cannotDeleteLastAdmin") : undefined} onClick={() => onConfirm({ title: t("deleteTitle"), description: t("deleteDescription"), confirmLabel: t("delete"), run: async () => { await usersService.delete(user.id); await onRefresh(); notify.success(t("deletedSuccess")); } })}>{t("delete")}</Button></Can>}
      {user.isDeleted && <Can permission="users.delete"><Button size="sm" variant="outline" onClick={() => onConfirm({ title: t("restoreTitle"), description: t("restoreDescription"), confirmLabel: t("restore"), run: async () => { await usersService.restore(user.id); await onRefresh(); notify.success(t("restored")); } })}>{t("restore")}</Button></Can>}
    </div>
  );
}
