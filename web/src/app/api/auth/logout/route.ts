import { NextResponse } from "next/server";
import { REFRESH_TOKEN_COOKIE, clearAuthCookies } from "@/lib/auth/cookies";
import { apiFetch } from "@/lib/api/client";
import { cookies } from "next/headers";

export async function POST() {
  const refreshToken = (await cookies()).get(REFRESH_TOKEN_COOKIE)?.value;
  const response = NextResponse.json({ message: "Sesión cerrada." });
  clearAuthCookies(response);

  if (!refreshToken) {
    return response;
  }

  const apiResponse = await apiFetch("auth/logout", undefined, {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
  });
  if (!apiResponse.ok && apiResponse.status !== 401) {
    const failure = NextResponse.json(
      { code: "logout_failed", message: "Se cerró la sesión local, pero no se pudo revocar en el servidor." },
      { status: 502 },
    );
    clearAuthCookies(failure);
    return failure;
  }

  return response;
}
