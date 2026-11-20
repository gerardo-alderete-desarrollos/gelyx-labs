import "server-only";

import { NextResponse } from "next/server";
import { clearAuthCookies, setAuthCookies } from "@/lib/auth/cookies";
import type { AuthTokens } from "@/features/auth/types/session";
import { correlationHeader } from "@/lib/correlation";

export async function responseFromApi(response: Response, tokens?: AuthTokens): Promise<NextResponse> {
  const body = response.status === 204 ? null : await response.text();
  const headers = new Headers();
  const contentType = response.headers.get("Content-Type");
  if (contentType) {
    headers.set("Content-Type", contentType);
  }
  const correlationId = response.headers.get(correlationHeader);
  if (correlationId) {
    headers.set(correlationHeader, correlationId);
  }
  const nextResponse = new NextResponse(body, { status: response.status, headers });
  if (tokens) {
    setAuthCookies(nextResponse, tokens);
  }
  return nextResponse;
}

export function expiredSessionResponse(): NextResponse {
  const response = NextResponse.json(
    { code: "session_expired", message: "La sesión expiró. Inicia sesión nuevamente." },
    { status: 401 },
  );
  clearAuthCookies(response);
  return response;
}
