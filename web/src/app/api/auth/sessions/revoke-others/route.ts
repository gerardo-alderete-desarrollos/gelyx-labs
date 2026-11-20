import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { REFRESH_TOKEN_COOKIE, setAuthCookies } from "@/lib/auth/cookies";
import { apiError } from "@/lib/api/client";
import { expiredSessionResponse } from "@/lib/auth/response";
import { sessionApiRequest } from "@/lib/auth/session-api";

export async function POST() {
  const refreshToken = (await cookies()).get(REFRESH_TOKEN_COOKIE)?.value;
  const result = await sessionApiRequest("auth/sessions/revoke-others", {
    method: "POST",
    headers: refreshToken ? { "X-Refresh-Token": refreshToken } : undefined,
  });
  if (result.expired) {
    return expiredSessionResponse();
  }
  if (!result.response.ok) {
    return NextResponse.json(await apiError(result.response), { status: result.response.status });
  }

  const response = new NextResponse(result.response.body, {
    status: result.response.status,
    headers: { "Content-Type": result.response.headers.get("Content-Type") ?? "application/json" },
  });
  if (result.tokens) {
    setAuthCookies(response, result.tokens);
  }
  return response;
}
