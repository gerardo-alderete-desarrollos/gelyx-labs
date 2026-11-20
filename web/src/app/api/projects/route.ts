import { proxyAuthenticatedRequest } from "@/lib/auth/proxy-request";
import { apiFetch } from "@/lib/api/client";

export async function GET() {
  const response = await apiFetch("projects");
  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers: response.headers,
  });
}

export function POST(request: Request) {
  return proxyAuthenticatedRequest(request, "projects");
}