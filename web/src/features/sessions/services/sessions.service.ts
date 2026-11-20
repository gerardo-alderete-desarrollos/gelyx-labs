"use client";

import { activeSessionsSchema, revokeOtherSessionsSchema } from "@/features/sessions/schemas/session.schema";
import type { ActiveSession, RevokeOtherSessionsResult } from "@/features/sessions/types/session";
import { fetchBff } from "@/lib/api/browser-client";

async function request(path: string, init?: RequestInit): Promise<Response> {
  const response = await fetchBff(path, init);
  if (response.ok) {
    return response;
  }

  const body: unknown = await response.json().catch(() => null);
  const code = typeof body === "object" && body !== null && "code" in body && typeof body.code === "string"
    ? body.code
    : "request_failed";
  throw new Error(code);
}

export async function getActiveSessions(): Promise<ActiveSession[]> {
  return activeSessionsSchema.parse(await (await request("/api/auth/sessions")).json());
}

export async function revokeSession(id: string): Promise<void> {
  await request(`/api/auth/sessions/${id}`, { method: "DELETE" });
}

export async function revokeOtherSessions(): Promise<RevokeOtherSessionsResult> {
  return revokeOtherSessionsSchema.parse(
    await (await request("/api/auth/sessions/revoke-others", { method: "POST" })).json(),
  );
}
