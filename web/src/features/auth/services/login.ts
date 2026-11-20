"use client";

import type { LoginFormValues } from "@/features/auth/schemas/auth.schema";
import type { SessionUser } from "@/features/auth/types/session";
import { fetchBff } from "@/lib/api/browser-client";

export type LoginResult = SessionUser | { twoFactorRequired: true; twoFactorToken: string };

export async function login(values: LoginFormValues): Promise<LoginResult> {
  let response: Response;
  try {
    response = await fetchBff("/api/auth/login", {
      method: "POST",
      body: JSON.stringify(values),
    });
  } catch {
    throw new Error("generic");
  }
  const body: unknown = await response.json();
  if (!response.ok) {
    const error = body as { code?: string };
    throw new Error(error.code ?? "generic");
  }
  const loginBody = body as { twoFactorRequired?: boolean; twoFactorToken?: string };
  if (loginBody.twoFactorRequired && loginBody.twoFactorToken) return { twoFactorRequired: true, twoFactorToken: loginBody.twoFactorToken };
  return body as SessionUser;
}
