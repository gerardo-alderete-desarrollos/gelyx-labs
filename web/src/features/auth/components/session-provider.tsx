"use client";

import { useSession } from "@/features/auth/hooks/use-session";
import type { SessionUser } from "@/features/auth/types/session";

export function SessionProvider({
  initialSession,
  children,
}: Readonly<{ initialSession: SessionUser; children: React.ReactNode }>) {
  useSession(initialSession);
  return children;
}
