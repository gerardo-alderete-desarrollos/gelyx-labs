"use client";

import { useSession } from "@/features/auth/hooks/use-session";

export function Can({
  permission,
  children,
}: Readonly<{ permission: string; children: React.ReactNode }>) {
  const { data: session } = useSession();
  return session?.permissions.includes(permission) ? children : null;
}
