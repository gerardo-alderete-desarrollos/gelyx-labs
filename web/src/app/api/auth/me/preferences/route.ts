import { NextResponse } from "next/server";
import { setAuthCookies } from "@/lib/auth/cookies";
import { sessionApiRequest } from "@/lib/auth/session-api";

export async function PUT(request: Request) {
  const result = await sessionApiRequest("auth/me/preferences", {
    method: "PUT",
    body: await request.text(),
  });
  if (result.expired) {
    return NextResponse.json({ code: "unauthorized" }, { status: 401 });
  }

  const response = new NextResponse(result.response.body, {
    status: result.response.status,
    headers: { "Content-Type": result.response.headers.get("Content-Type") ?? "application/json" },
  });
  if (result.tokens) {
    setAuthCookies(response, result.tokens);
  }
  return response;
}
