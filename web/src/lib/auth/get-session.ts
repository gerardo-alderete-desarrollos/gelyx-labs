import "server-only";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { sessionUserSchema } from "@/features/auth/schemas/session.schema";
import type { SessionUser } from "@/features/auth/types/session";
import { apiFetch } from "@/lib/api/client";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies";

export async function getSession(returnUrl = "/dashboard"): Promise<SessionUser> {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(ACCESS_TOKEN_COOKIE)?.value;
  const refreshToken = cookieStore.get(REFRESH_TOKEN_COOKIE)?.value;

  if (!accessToken) {
    if (refreshToken) {
      redirect(`/api/auth/refresh?returnUrl=${encodeURIComponent(returnUrl)}`);
    }
    redirect(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  }

  const response = await apiFetch("auth/me", accessToken);
  if (response.status === 401) {
    if (refreshToken) {
      redirect(`/api/auth/refresh?returnUrl=${encodeURIComponent(returnUrl)}`);
    }
    redirect(`/login?reason=session-expired&returnUrl=${encodeURIComponent(returnUrl)}`);
  }
  if (!response.ok) {
    throw new Error(`Could not load the current session (${response.status}).`);
  }

  return sessionUserSchema.parse(await response.json());
}

export async function requireModule(moduleKey: string): Promise<SessionUser> {
  const session = await getSession(`/dashboard/${moduleKey}`);
  if (!session.modules.some((module) => module.key === moduleKey)) {
    redirect("/403");
  }
  return session;
}

export async function requirePermission(permission: string): Promise<SessionUser> {
  const session = await getSession("/dashboard");
  if (!session.permissions.includes(permission)) {
    redirect("/403");
  }
  return session;
}
