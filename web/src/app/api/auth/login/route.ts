import { NextResponse } from "next/server";
import {
  authTokensSchema,
  sessionUserSchema,
  twoFactorChallengeSchema,
} from "@/features/auth/schemas/session.schema";
import { loginSchema } from "@/features/auth/schemas/auth.schema";
import { apiError, apiFetch } from "@/lib/api/client";
import { setAuthCookies } from "@/lib/auth/cookies";

export async function POST(request: Request) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json(
      { code: "invalid_request", message: "El formato de la solicitud no es válido." },
      { status: 400 },
    );
  }

  const parsed = loginSchema.safeParse(body);
  if (!parsed.success) {
    return NextResponse.json(
      { code: "validation_failed", message: parsed.error.issues[0]?.message ?? "Revisa los datos ingresados." },
      { status: 422 },
    );
  }

  const { rememberMe, ...loginData } = parsed.data;

  const loginResponse = await apiFetch("auth/login", undefined, {
    method: "POST",
    body: JSON.stringify(loginData),
  });
  if (!loginResponse.ok) {
    const error = await apiError(loginResponse);
    return NextResponse.json(error, { status: loginResponse.status });
  }

  const loginResult: unknown = await loginResponse.json();
  const challengeResult = twoFactorChallengeSchema.safeParse(loginResult);
  if (challengeResult.success) {
    return NextResponse.json(challengeResult.data);
  }

  const tokenResult = authTokensSchema.safeParse(loginResult);
  if (!tokenResult.success) {
    return NextResponse.json(
      { code: "invalid_api_response", message: "La respuesta de autenticación no es válida." },
      { status: 502 },
    );
  }

  const meResponse = await apiFetch("auth/me", tokenResult.data.accessToken);
  if (!meResponse.ok) {
    const error = await apiError(meResponse);
    return NextResponse.json(error, { status: meResponse.status });
  }

  const sessionResult = sessionUserSchema.safeParse(await meResponse.json());
  if (!sessionResult.success) {
    return NextResponse.json(
      { code: "invalid_api_response", message: "La respuesta de sesión no es válida." },
      { status: 502 },
    );
  }

  const response = NextResponse.json(sessionResult.data);
  setAuthCookies(response, tokenResult.data, rememberMe === true);
  response.cookies.set("NEXT_LOCALE", sessionResult.data.locale, {
    path: "/",
    maxAge: 60 * 60 * 24 * 365,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
  });
  return response;
}
