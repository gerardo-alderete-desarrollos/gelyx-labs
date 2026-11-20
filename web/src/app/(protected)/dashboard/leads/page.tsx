import { LeadsPage } from "@/features/leads/components/leads-page";
import { requireModule } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("leads");
  return { title: t("metadata") };
}

export default async function Page() {
  await requireModule("leads");
  return <LeadsPage />;
}