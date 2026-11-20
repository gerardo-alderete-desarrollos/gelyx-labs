import { ModulesPage } from "@/features/modules/components/modules-page";
import { requireModule } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("modules");
  return { title: t("metadata") };
}

export default async function ModuleManagementPage() {
  await requireModule("modules");
  return <ModulesPage />;
}
