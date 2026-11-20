"use client";

import { useQuery } from "@tanstack/react-query";
import type { SessionUser } from "@/features/auth/types/session";
import { fetchBff } from "@/lib/api/browser-client";

export const sessionQueryKey = ["auth", "me"] as const;

async function fetchSession(): Promise<SessionUser> {
  const response = await fetchBff("/api/auth/me");
  if (!response.ok) {
    throw new Error("No se pudo validar la sesión.");
  }
  return (await response.json()) as SessionUser;
}

export function useSession(initialSession?: SessionUser) {
  return useQuery({
    queryKey: sessionQueryKey,
    queryFn: fetchSession,
    initialData: initialSession,
    staleTime: 0,
    refetchOnWindowFocus: true,
  });
}
