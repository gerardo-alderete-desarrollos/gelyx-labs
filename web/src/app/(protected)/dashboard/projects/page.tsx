import { ProjectsPage } from "@/features/projects/components/projects-page";
import { requireModule } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("projects");
  return { title: t("metadata") };
}

export default async function Page() {
  await requireModule("projects");
  return <ProjectsPage />;
}