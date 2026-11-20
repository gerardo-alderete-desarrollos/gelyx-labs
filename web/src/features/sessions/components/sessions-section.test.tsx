import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { SessionsSection } from "@/features/sessions/components/sessions-section";
import {
  getActiveSessions,
  revokeOtherSessions,
  revokeSession,
} from "@/features/sessions/services/sessions.service";
import { notify } from "@/lib/notify";

vi.mock("@/features/sessions/services/sessions.service", () => ({
  getActiveSessions: vi.fn(),
  revokeSession: vi.fn(),
  revokeOtherSessions: vi.fn(),
}));

vi.mock("@/lib/notify", () => ({
  notify: { error: vi.fn(), success: vi.fn() },
}));

const sessions = [
  {
    id: "b1d1f0e4-84de-49dd-8c3a-8c621fdb8f5e",
    createdAt: "2026-10-07T08:00:00+00:00",
    expiresAt: "2026-10-14T08:00:00+00:00",
    createdByIp: "203.0.113.10",
    agent: "Firefox on Linux",
    current: true,
  },
  {
    id: "a2d1f0e4-84de-49dd-8c3a-8c621fdb8f5e",
    createdAt: "2026-10-06T08:00:00+00:00",
    expiresAt: "2026-10-13T08:00:00+00:00",
    createdByIp: "198.51.100.8",
    agent: "Safari on iPhone",
    current: false,
  },
];

function renderSection() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <SessionsSection />
    </QueryClientProvider>,
  );
}

describe("SessionsSection", () => {
  afterEach(cleanup);

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getActiveSessions).mockResolvedValue(sessions);
  });

  it("shows session metadata and identifies the current session", async () => {
    renderSection();

    expect(await screen.findByText("Firefox on Linux")).toBeInTheDocument();
    expect(screen.getByText("Safari on iPhone")).toBeInTheDocument();
    expect(screen.getByText("Esta sesión")).toBeInTheDocument();
    expect(screen.getByText("203.0.113.10")).toBeInTheDocument();
  });

  it("revokes one session then refreshes the active-sessions query", async () => {
    vi.mocked(revokeSession).mockResolvedValue(undefined);
    renderSection();

    fireEvent.click(await screen.findByRole("button", { name: "Cerrar sesión en Safari on iPhone" }));

    await waitFor(() => {
      expect(revokeSession).toHaveBeenCalledWith(sessions[1].id);
      expect(notify.success).toHaveBeenCalledWith("La sesión se cerró.");
    });
    expect(getActiveSessions).toHaveBeenCalledTimes(2);
  });

  it("revokes every other session", async () => {
    vi.mocked(revokeOtherSessions).mockResolvedValue({ revokedCount: 1 });
    renderSection();

    await screen.findByText("Safari on iPhone");
    fireEvent.click(await screen.findByRole("button", { name: "Cerrar las demás" }));

    await waitFor(() => {
      expect(revokeOtherSessions).toHaveBeenCalledOnce();
      expect(notify.success).toHaveBeenCalledWith("Se cerraron 1 sesiones.");
    });
  });
});
