import "server-only";

import { cookies } from "next/headers";
import { apiFetch } from "@/lib/api/client";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies";
import { refreshTokens, UpstreamRefreshError } from "@/lib/auth/refresh";
import type { AuthTokens } from "@/features/auth/types/session";

export interface SessionApiResult {
  response: Response;
  tokens?: AuthTokens;
  expired: boolean;
}

export async function sessionApiRequest(
  path: string,
  init: RequestInit = {},
): Promise<SessionApiResult> {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(ACCESS_TOKEN_COOKIE)?.value;
  const refreshToken = cookieStore.get(REFRESH_TOKEN_COOKIE)?.value;

  let response = accessToken
    ? await apiFetch(path, accessToken, init)
    : new Response(null, { status: 401 });
  let tokens: AuthTokens | undefined;

  if (response.status === 401 && refreshToken) {
    try {
      tokens = await refreshTokens(refreshToken);
    } catch (error) {
      if (error instanceof UpstreamRefreshError && error.status >= 400 && error.status < 500) {
        return { response: new Response(null, { status: 401 }), expired: true };
      }
      throw error;
    }
    const headers = new Headers(init.headers);
    if (headers.has("X-Refresh-Token")) {
      headers.set("X-Refresh-Token", tokens.refreshToken);
    }
    response = await apiFetch(path, tokens.accessToken, { ...init, headers });
  }

  return {
    response,
    tokens,
    expired: response.status === 401,
  };
}
