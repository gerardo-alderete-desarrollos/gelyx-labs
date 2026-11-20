import { proxyAuthenticatedRequest } from "@/lib/auth/proxy-request";

export function GET(request: Request) {
  return proxyAuthenticatedRequest(request, "users");
}

export function POST(request: Request) {
  return proxyAuthenticatedRequest(request, "users");
}
