import { NextRequest, NextResponse } from "next/server";

const protectedPaths = ["/dashboard", "/change-password", "/403"];
const publicAuthPaths = ["/login", "/forgot-password", "/reset-password"];

export function proxy(request: NextRequest) {
  const pathname = request.nextUrl.pathname;
  const hasSessionCookie = Boolean(
    request.cookies.get("gelyx-labs_access")?.value ||
      request.cookies.get("gelyx-labs_refresh")?.value,
  );

  if (
    protectedPaths.some((path) => pathname === path || pathname.startsWith(`${path}/`)) &&
    !hasSessionCookie
  ) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("returnUrl", `${pathname}${request.nextUrl.search}`);
    return NextResponse.redirect(loginUrl);
  }

  if (publicAuthPaths.includes(pathname)) {
    const accessToken = request.cookies.get("gelyx-labs_access")?.value;
    if (accessToken) {
      return NextResponse.redirect(new URL("/dashboard", request.url));
    }
    if (pathname === "/login" && request.cookies.get("gelyx-labs_refresh")?.value) {
      return NextResponse.redirect(
        new URL(
          `/api/auth/refresh?returnUrl=${encodeURIComponent("/dashboard")}`,
          request.url,
        ),
      );
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!api|_next|_vercel|.*\\..*).*)"],
};