import { proxyAuthenticatedRequest } from "@/lib/auth/proxy-request";

export function GET(request: Request) {
  return proxyAuthenticatedRequest(request, "roles");
}

export function POST(request: Request) {
  return proxyAuthenticatedRequest(request, "roles");
}
