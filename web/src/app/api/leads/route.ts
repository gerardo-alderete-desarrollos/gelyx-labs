import { proxyAuthenticatedRequest } from "@/lib/auth/proxy-request";
import { apiFetch } from "@/lib/api/client";

export function GET(request: Request) {
  return proxyAuthenticatedRequest(request, "leads");
}

export async function POST(request: Request) {
  const body = await request.text();
  const response = await apiFetch("leads", undefined, {
    method: "POST",
    body,
    headers: { "Content-Type": "application/json" },
  });
  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers: response.headers,
  });
}