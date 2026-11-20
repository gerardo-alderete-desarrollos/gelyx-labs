import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api/client", () => ({
  apiError: vi.fn(),
  apiFetch: vi.fn(),
}));
vi.mock("@/lib/auth/cookies", () => ({
  setAuthCookies: vi.fn(),
}));

import { POST } from "@/app/api/auth/login/route";
import { apiFetch } from "@/lib/api/client";
import { setAuthCookies } from "@/lib/auth/cookies";

describe("POST /api/auth/login", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(apiFetch).mockReset();
    vi.mocked(setAuthCookies).mockReset();
  });

  it("forwards a two-factor challenge without setting authentication cookies", async () => {
    const challenge = {
      accessToken: null,
      refreshToken: null,
      expiresIn: 0,
      twoFactorRequired: true,
      twoFactorToken: "temporary-two-factor-token",
    };
    vi.mocked(apiFetch).mockResolvedValueOnce(new Response(JSON.stringify(challenge)));

    const response = await POST(new Request("http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "person@example.com", password: "password" }),
    }));

    await expect(response.json()).resolves.toEqual(challenge);
    expect(apiFetch).toHaveBeenCalledTimes(1);
    expect(setAuthCookies).not.toHaveBeenCalled();
  });

  it("sets cookies with maxAge when rememberMe is true", async () => {
    const tokens = { accessToken: "at", refreshToken: "rt", expiresIn: 900 };
    const session = {
      id: "01a11b30-63df-732d-a095-83fe42a019b9",
      email: "test@example.com",
      fullName: "Test User",
      roles: ["Usuario"],
      permissions: ["users.read"],
      modules: [{ key: "dashboard", name: "Panel", icon: null, route: "/dashboard", sortOrder: 0 }],
      mustChangePassword: false,
      locale: "es",
      theme: "system",
    };
    vi.mocked(apiFetch)
      .mockResolvedValueOnce(new Response(JSON.stringify(tokens)))
      .mockResolvedValueOnce(new Response(JSON.stringify(session)));

    const request = new Request("http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "test@example.com", password: "password", rememberMe: true }),
    });
    const response = await POST(request);

    const responseBody = await response.json();
    console.log("Response status:", response.status, "body:", responseBody);
    expect(response.ok).toBe(true);
    expect(setAuthCookies).toHaveBeenCalledTimes(1);
    const [, , rememberMeArg] = vi.mocked(setAuthCookies).mock.calls[0];
    expect(rememberMeArg).toBe(true);
  });

  it("sets session cookies when rememberMe is false", async () => {
    const tokens = { accessToken: "at", refreshToken: "rt", expiresIn: 900 };
    const session = {
      id: "01a11b30-63df-732d-a095-83fe42a019b9",
      email: "test@example.com",
      fullName: "Test User",
      roles: ["Usuario"],
      permissions: ["users.read"],
      modules: [{ key: "dashboard", name: "Panel", icon: null, route: "/dashboard", sortOrder: 0 }],
      mustChangePassword: false,
      locale: "es",
      theme: "system",
    };
    vi.mocked(apiFetch)
      .mockResolvedValueOnce(new Response(JSON.stringify(tokens)))
      .mockResolvedValueOnce(new Response(JSON.stringify(session)));

    const request = new Request("http://localhost/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: "test@example.com", password: "password", rememberMe: false }),
    });
    const response = await POST(request);

    const responseBody = await response.json();
    console.log("Response status:", response.status, "body:", responseBody);
    expect(response.ok).toBe(true);
    expect(setAuthCookies).toHaveBeenCalledTimes(1);
    const [, , rememberMeArg] = vi.mocked(setAuthCookies).mock.calls[0];
    expect(rememberMeArg).toBe(false);
  });
});
