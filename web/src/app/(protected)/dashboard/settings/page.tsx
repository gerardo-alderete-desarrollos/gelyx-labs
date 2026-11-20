import { SettingsPage } from "@/features/settings/components/settings-page";
import { requireModule, requirePermission } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("settings");
  return { title: t("metadata") };
}

export default async function SettingsManagementPage() {
  await requireModule("settings");
  await requirePermission("settings.read");
  return <SettingsPage />;
}
