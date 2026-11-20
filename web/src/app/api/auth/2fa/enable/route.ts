import { NextResponse } from "next/server";
import { apiError } from "@/lib/api/client";
import { sessionApiRequest } from "@/lib/auth/session-api";
import { expiredSessionResponse } from "@/lib/auth/response";

export async function POST(request: Request) {
  const body = await request.json().catch(() => null);
  if (!body || typeof body !== "object") return NextResponse.json({ code: "invalid_request" }, { status: 400 });
  const result = await sessionApiRequest("auth/2fa/enable", { method: "POST", body: JSON.stringify(body) });
  if (result.expired) return expiredSessionResponse();
  return result.response.ok ? NextResponse.json(await result.response.json()) : NextResponse.json(await apiError(result.response), { status: result.response.status });
}
