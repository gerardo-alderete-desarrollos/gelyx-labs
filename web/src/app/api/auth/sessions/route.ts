import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { REFRESH_TOKEN_COOKIE, setAuthCookies } from "@/lib/auth/cookies";
import { apiError } from "@/lib/api/client";
import { expiredSessionResponse } from "@/lib/auth/response";
import { sessionApiRequest, type SessionApiResult } from "@/lib/auth/session-api";

function responseFrom(result: SessionApiResult): Promise<NextResponse> | NextResponse {
  if (result.expired) {
    return expiredSessionResponse();
  }

  if (!result.response.ok) {
    return apiError(result.response).then((error) =>
      NextResponse.json(error, { status: result.response.status }));
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

export async function GET() {
  const refreshToken = (await cookies()).get(REFRESH_TOKEN_COOKIE)?.value;
  const result = await sessionApiRequest("auth/sessions", {
    headers: refreshToken ? { "X-Refresh-Token": refreshToken } : undefined,
  });
  return responseFrom(result);
}
