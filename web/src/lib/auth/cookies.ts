import "server-only";

import type { NextResponse } from "next/server";
import type { AuthTokens } from "@/features/auth/types/session";

export const ACCESS_TOKEN_COOKIE = "gelyx-labs_access";
export const REFRESH_TOKEN_COOKIE = "gelyx-labs_refresh";

const refreshLifetimeSeconds = 7 * 24 * 60 * 60;

function cookieOptions(maxAge: number) {
  return {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
    maxAge,
  };
}

export function setAuthCookies(response: NextResponse, tokens: AuthTokens, rememberMe = false): void {
  const accessMaxAge = rememberMe ? refreshLifetimeSeconds : Number(tokens.expiresIn);
  response.cookies.set(ACCESS_TOKEN_COOKIE, tokens.accessToken, cookieOptions(accessMaxAge));
  response.cookies.set(
    REFRESH_TOKEN_COOKIE,
    tokens.refreshToken,
    cookieOptions(refreshLifetimeSeconds),
  );
}

export function clearAuthCookies(response: NextResponse): void {
  response.cookies.set(ACCESS_TOKEN_COOKIE, "", { ...cookieOptions(0), expires: new Date(0) });
  response.cookies.set(REFRESH_TOKEN_COOKIE, "", { ...cookieOptions(0), expires: new Date(0) });
}
