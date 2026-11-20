import { NextResponse } from "next/server";
import { forgotPasswordSchema } from "@/features/auth/schemas/auth.schema";
import { apiError, apiFetch } from "@/lib/api/client";

export async function POST(request: Request) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ code: "invalid_request", message: "La solicitud no es válida." }, { status: 400 });
  }
  const parsed = forgotPasswordSchema.safeParse(body);
  if (!parsed.success) {
    return NextResponse.json({ code: "validation_failed", message: "Ingresa un correo válido." }, { status: 422 });
  }

  const response = await apiFetch("auth/forgot-password", undefined, {
    method: "POST",
    body: JSON.stringify(parsed.data),
  });
  if (response.ok) {
    return NextResponse.json({ message: "Si el correo está registrado, recibirás instrucciones." });
  }
  return NextResponse.json(await apiError(response), { status: response.status });
}
