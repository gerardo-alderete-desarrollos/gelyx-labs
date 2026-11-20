import { NextResponse } from "next/server";
import { resetPasswordSchema } from "@/features/auth/schemas/auth.schema";
import { apiError, apiFetch } from "@/lib/api/client";

export async function POST(request: Request) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ code: "invalid_request", message: "La solicitud no es válida." }, { status: 400 });
  }
  const parsed = resetPasswordSchema.safeParse(body);
  if (!parsed.success) {
    return NextResponse.json(
      { code: "validation_failed", message: parsed.error.issues[0]?.message ?? "Revisa los datos ingresados." },
      { status: 422 },
    );
  }

  const response = await apiFetch("auth/reset-password", undefined, {
    method: "POST",
    body: JSON.stringify(parsed.data),
  });
  if (response.ok) {
    return NextResponse.json({ message: "La contraseña se actualizó. Ya puedes iniciar sesión." });
  }
  return NextResponse.json(await apiError(response), { status: response.status });
}
