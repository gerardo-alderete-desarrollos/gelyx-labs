import { NextResponse } from "next/server";
import { profileSchema } from "@/features/profile/schemas/profile.schema";
import { expiredSessionResponse, responseFromApi } from "@/lib/auth/response";
import { sessionApiRequest } from "@/lib/auth/session-api";

export async function PUT(request: Request) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ code: "invalid_request", message: "La solicitud no es válida." }, { status: 400 });
  }
  const parsed = profileSchema.safeParse(body);
  if (!parsed.success) {
    return NextResponse.json(
      { code: "validation_failed", message: parsed.error.issues[0]?.message ?? "Revisa los datos ingresados." },
      { status: 422 },
    );
  }

  const result = await sessionApiRequest("profile/me", {
    method: "PUT",
    body: JSON.stringify(parsed.data),
  });
  if (result.expired) {
    return expiredSessionResponse();
  }
  return responseFromApi(result.response, result.tokens);
}
