import { beforeEach, describe, expect, it, vi } from "vitest";

const { fetchBff } = vi.hoisted(() => ({ fetchBff: vi.fn() }));
vi.mock("@/lib/api/browser-client", () => ({ fetchBff }));

import { settingValues, settingsService } from "@/features/settings/services/settings.service";

describe("settingsService", () => {
  beforeEach(() => fetchBff.mockReset());

  it("updates a setting through the BFF with its JSON value", async () => {
    fetchBff.mockResolvedValue(new Response(JSON.stringify({
      key: "auth.allowSelfRegistration", value: true, type: "boolean", updatedAt: "2026-10-07T08:00:00+00:00", updatedBy: null,
    }), { status: 200 }));

    await expect(settingsService.update("auth.allowSelfRegistration", true)).resolves.toMatchObject({ value: true });
    expect(fetchBff).toHaveBeenCalledWith("/api/settings/auth.allowSelfRegistration", {
      method: "PUT", body: JSON.stringify({ value: true }),
    });
  });

  it("maps public setting values by key without exposing unspecified values", () => {
    expect(settingValues([{ key: "flags.beta", value: true, type: "boolean", updatedAt: "2026-10-07T08:00:00+00:00", updatedBy: null }]))
      .toEqual({ "flags.beta": true });
  });
});
