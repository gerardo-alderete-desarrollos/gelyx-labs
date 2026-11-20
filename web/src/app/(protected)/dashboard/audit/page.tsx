import { AuditPage } from "@/features/audit/components/audit-page";
import { requireModule, requirePermission } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("audit");
  return { title: t("metadata") };
}

export default async function AuditRoute({
  searchParams,
}: Readonly<{ searchParams: Promise<Record<string, string | string[] | undefined>> }>) {
  await requireModule("audit");
  await requirePermission("audit.read");
  const values = await searchParams;
  const initialQuery = Object.fromEntries(
    Object.entries(values).map(([key, value]) => [key, Array.isArray(value) ? value[0] : value]),
  );
  return <AuditPage key={JSON.stringify(initialQuery)} initialQuery={initialQuery} />;
}
