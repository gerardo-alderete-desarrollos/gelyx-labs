import { NextResponse } from "next/server";
import { ACCESS_TOKEN_COOKIE, REFRESH_TOKEN_COOKIE, clearAuthCookies } from "@/lib/auth/cookies";
import { apiError } from "@/lib/api/client";
import { cookies } from "next/headers";
import { changePasswordSchema } from "@/features/auth/schemas/auth.schema";
import { sessionApiRequest } from "@/lib/auth/session-api";
import { expiredSessionResponse } from "@/lib/auth/response";

export async function POST(request: Request) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ code: "invalid_request", message: "La solicitud no es válida." }, { status: 400 });
  }

  const parsed = changePasswordSchema.safeParse(body);
  if (!parsed.success) {
    return NextResponse.json(
      { code: "validation_failed", message: parsed.error.issues[0]?.message ?? "Revisa los datos ingresados." },
      { status: 422 },
    );
  }

  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_TOKEN_COOKIE)?.value;
  const accessToken = cookieStore.get(ACCESS_TOKEN_COOKIE)?.value;
  if (!refreshToken || !accessToken) {
    return expiredSessionResponse();
  }

  const result = await sessionApiRequest("auth/change-password", {
    method: "POST",
    body: JSON.stringify({
      currentPassword: parsed.data.currentPassword,
      newPassword: parsed.data.newPassword,
      refreshToken,
    }),
  });
  if (result.expired) {
    return expiredSessionResponse();
  }

  const response = result.response.ok
    ? new NextResponse(null, { status: result.response.status })
    : NextResponse.json(await apiError(result.response), { status: result.response.status });
  if (result.response.ok) {
    clearAuthCookies(response);
  }
  return response;
}
