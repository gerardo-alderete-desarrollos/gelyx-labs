import { beforeEach, describe, expect, it, vi } from "vitest";
import { getActiveSessions, revokeOtherSessions, revokeSession } from "@/features/sessions/services/sessions.service";
import { fetchBff } from "@/lib/api/browser-client";

vi.mock("@/lib/api/browser-client", () => ({
  fetchBff: vi.fn(),
}));

const activeSession = {
  id: "b1d1f0e4-84de-49dd-8c3a-8c621fdb8f5e",
  createdAt: "2026-10-07T08:00:00+00:00",
  expiresAt: "2026-10-14T08:00:00+00:00",
  createdByIp: "203.0.113.10",
  agent: "Firefox on Linux",
  current: true,
};

describe("sessions service", () => {
  beforeEach(() => vi.clearAllMocks());

  it("gets active sessions exclusively through the sessions BFF route", async () => {
    vi.mocked(fetchBff).mockResolvedValue(new Response(JSON.stringify([activeSession])));

    await expect(getActiveSessions()).resolves.toEqual([activeSession]);
    expect(fetchBff).toHaveBeenCalledWith("/api/auth/sessions", undefined);
  });

  it("uses the BFF routes to revoke one session or every other session", async () => {
    vi.mocked(fetchBff)
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ revokedCount: 2 })));

    await expect(revokeSession(activeSession.id)).resolves.toBeUndefined();
    await expect(revokeOtherSessions()).resolves.toEqual({ revokedCount: 2 });

    expect(fetchBff).toHaveBeenNthCalledWith(1, `/api/auth/sessions/${activeSession.id}`, { method: "DELETE" });
    expect(fetchBff).toHaveBeenNthCalledWith(2, "/api/auth/sessions/revoke-others", { method: "POST" });
  });

  it("exposes the API error code to the notification layer", async () => {
    vi.mocked(fetchBff).mockResolvedValue(
      new Response(JSON.stringify({ code: "session_not_found" }), { status: 404 }),
    );

    await expect(revokeSession(activeSession.id)).rejects.toThrow("session_not_found");
  });
});
