import { NextResponse } from "next/server";
import { apiError } from "@/lib/api/client";
import { sessionApiRequest } from "@/lib/auth/session-api";
import { expiredSessionResponse } from "@/lib/auth/response";

export async function POST() {
  const result = await sessionApiRequest("auth/2fa/setup", { method: "POST" });
  if (result.expired) return expiredSessionResponse();
  return result.response.ok
    ? NextResponse.json(await result.response.json())
    : NextResponse.json(await apiError(result.response), { status: result.response.status });
}
