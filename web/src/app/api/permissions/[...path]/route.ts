import { proxyAuthenticatedRequest } from "@/lib/auth/proxy-request";

type RouteContext = Readonly<{ params: Promise<{ path: string[] }> }>;

async function proxy(request: Request, context: RouteContext) {
  const { path } = await context.params;
  if (path.length === 0 || path.some((segment) => !/^[\w-]+$/.test(segment))) {
    return Response.json({ code: "invalid_path", message: "La ruta solicitada no es válida." }, { status: 400 });
  }

  return proxyAuthenticatedRequest(request, `permissions/${path.map(encodeURIComponent).join("/")}`);
}

export const GET = proxy;
export const PUT = proxy;
