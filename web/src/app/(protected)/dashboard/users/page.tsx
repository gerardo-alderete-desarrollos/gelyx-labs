import { UsersPage } from "@/features/users/components/users-page";
import { requireModule, requirePermission } from "@/lib/auth/get-session";
import { getTranslations } from "next-intl/server";

export async function generateMetadata() {
  const t = await getTranslations("users");
  return { title: t("metadata") };
}

type SearchParams = Promise<Record<string, string | string[] | undefined>>;

export default async function UsersRoute({ searchParams }: Readonly<{ searchParams: SearchParams }>) {
  await requireModule("users");
  await requirePermission("users.read");
  const values = await searchParams;
  const initialQuery = Object.fromEntries(
    Object.entries(values).map(([key, value]) => [key, Array.isArray(value) ? value[0] : value]),
  );
  return <UsersPage key={JSON.stringify(initialQuery)} initialQuery={initialQuery} />;
}
