import { proxyAuthenticatedRequest } from "@/lib/auth/proxy-request";

type RouteContext = Readonly<{ params: Promise<{ key: string }> }>;

export async function PUT(request: Request, { params }: RouteContext) {
  const { key } = await params;
  return proxyAuthenticatedRequest(request, `settings/${encodeURIComponent(key)}`);
}
