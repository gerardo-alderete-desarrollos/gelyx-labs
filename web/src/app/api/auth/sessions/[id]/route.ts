import { NextResponse } from "next/server";
import { apiError } from "@/lib/api/client";
import { expiredSessionResponse } from "@/lib/auth/response";
import { sessionApiRequest } from "@/lib/auth/session-api";

export async function DELETE(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const result = await sessionApiRequest(`auth/sessions/${id}`, { method: "DELETE" });
  if (result.expired) {
    return expiredSessionResponse();
  }
  if (!result.response.ok) {
    return NextResponse.json(await apiError(result.response), { status: result.response.status });
  }
  return new NextResponse(null, { status: result.response.status });
}
