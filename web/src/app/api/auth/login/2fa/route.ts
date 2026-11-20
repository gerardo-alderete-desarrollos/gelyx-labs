import { NextResponse } from "next/server";
import { apiError, apiFetch } from "@/lib/api/client";
import { authTokensSchema, sessionUserSchema } from "@/features/auth/schemas/session.schema";
import { setAuthCookies } from "@/lib/auth/cookies";

export async function POST(request: Request) {
  const body = await request.json().catch(() => null);
  if (!body || typeof body !== "object") return NextResponse.json({ code: "invalid_request" }, { status: 400 });
  const login = await apiFetch("auth/login/2fa", undefined, { method: "POST", body: JSON.stringify(body) });
  if (!login.ok) return NextResponse.json(await apiError(login), { status: login.status });
  const tokens = authTokensSchema.safeParse(await login.json());
  if (!tokens.success) return NextResponse.json({ code: "invalid_api_response" }, { status: 502 });
  const me = await apiFetch("auth/me", tokens.data.accessToken);
  if (!me.ok) return NextResponse.json(await apiError(me), { status: me.status });
  const session = sessionUserSchema.safeParse(await me.json());
  if (!session.success) return NextResponse.json({ code: "invalid_api_response" }, { status: 502 });
  const response = NextResponse.json(session.data);
  setAuthCookies(response, tokens.data);
  response.cookies.set("NEXT_LOCALE", session.data.locale, { path: "/", maxAge: 31536000, sameSite: "lax", secure: process.env.NODE_ENV === "production" });
  return response;
}
