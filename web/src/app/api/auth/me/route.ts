import { NextResponse } from "next/server";
import { sessionUserSchema } from "@/features/auth/schemas/session.schema";
import { expiredSessionResponse } from "@/lib/auth/response";
import { setAuthCookies } from "@/lib/auth/cookies";
import { sessionApiRequest } from "@/lib/auth/session-api";

export async function GET() {
  const result = await sessionApiRequest("auth/me");
  if (result.expired) {
    return expiredSessionResponse();
  }

  const body = await result.response.json();
  const parsed = sessionUserSchema.safeParse(body);
  if (!parsed.success) {
    return NextResponse.json(
      { code: "invalid_api_response", message: "La respuesta de sesión no es válida." },
      { status: 502 },
    );
  }
  const response = NextResponse.json(parsed.data, { status: result.response.status });
  if (result.tokens) {
    setAuthCookies(response, result.tokens);
  }
  return response;
}
