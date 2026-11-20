import { RolesPage } from "@/features/roles/components/roles-page";
import { requireModule, requirePermission } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("roles");
  return { title: t("metadata") };
}

export default async function RoleManagementPage() {
  await requireModule("roles");
  await requirePermission("roles.read");
  return <RolesPage />;
}
