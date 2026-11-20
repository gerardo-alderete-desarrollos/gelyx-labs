import { NextResponse } from "next/server";
import { sessionUserSchema, authTokensSchema } from "@/features/auth/schemas/session.schema";
import { REFRESH_TOKEN_COOKIE, clearAuthCookies, setAuthCookies } from "@/lib/auth/cookies";
import { apiFetch } from "@/lib/api/client";
import { refreshTokens, UpstreamRefreshError } from "@/lib/auth/refresh";
import { safeReturnUrl } from "@/lib/auth/return-url";
import { sessionApiRequest } from "@/lib/auth/session-api";
import { expiredSessionResponse } from "@/lib/auth/response";
import { cookies } from "next/headers";

export async function POST() {
  const result = await sessionApiRequest("auth/me");
  if (result.expired) {
    return expiredSessionResponse();
  }
  const parsed = sessionUserSchema.safeParse(await result.response.json());
  if (!parsed.success) {
    return NextResponse.json(
      { code: "invalid_api_response", message: "La respuesta de sesión no es válida." },
      { status: 502 },
    );
  }
  const response = NextResponse.json(parsed.data);
  if (result.tokens) {
    setAuthCookies(response, result.tokens);
  }
  return response;
}

export async function GET(request: Request) {
  const returnUrl = safeReturnUrl(new URL(request.url).searchParams.get("returnUrl"));
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_TOKEN_COOKIE)?.value;
  if (!refreshToken) {
    const response = NextResponse.redirect(new URL(
      `/login?reason=session-expired&returnUrl=${encodeURIComponent(returnUrl)}`,
      request.url,
    ));
    clearAuthCookies(response);
    return response;
  }

  try {
    const tokenResult = authTokensSchema.safeParse(await refreshTokens(refreshToken));
    if (!tokenResult.success) {
      throw new Error("The token refresh response was invalid.");
    }
    const meResponse = await apiFetch("auth/me", tokenResult.data.accessToken);
    if (!meResponse.ok) {
      if (meResponse.status >= 400 && meResponse.status < 500) {
        throw new UpstreamRefreshError(meResponse.status);
      }
      throw new Error(`Session validation failed with status ${meResponse.status}.`);
    }
    sessionUserSchema.parse(await meResponse.json());

    const response = NextResponse.redirect(new URL(returnUrl, request.url));
    setAuthCookies(response, tokenResult.data);
    return response;
  } catch (error) {
    if (!(error instanceof UpstreamRefreshError) || error.status < 400 || error.status >= 500) {
      return NextResponse.json(
        { code: "auth_service_unavailable", message: "No se pudo validar la sesión. Intenta nuevamente." },
        { status: 503 },
      );
    }
    const response = NextResponse.redirect(new URL(
      `/login?reason=session-expired&returnUrl=${encodeURIComponent(returnUrl)}`,
      request.url,
    ));
    clearAuthCookies(response);
    return response;
  }
}
