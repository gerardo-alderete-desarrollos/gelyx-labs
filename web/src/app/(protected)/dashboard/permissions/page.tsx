import { PermissionsMatrix } from "@/features/permissions/components/permissions-matrix";
import { requireModule, requirePermission } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("permissions");
  return { title: t("metadata") };
}

export default async function PermissionsPage({
  searchParams,
}: Readonly<{ searchParams: Promise<{ roleId?: string }> }>) {
  await requireModule("permissions");
  await requirePermission("permissions.read");
  const { roleId } = await searchParams;
  return <PermissionsMatrix focusedRoleId={roleId} />;
}
