import { describe, expect, it, vi } from "vitest";
import requestConfig from "@/i18n/request";

vi.mock("next-intl/server", () => ({
  getRequestConfig: <T>(config: T) => config,
}));

describe("requestConfig", () => {
  it("uses the request locale when valid", async () => {
    const config = await requestConfig({ requestLocale: Promise.resolve("en") });
    expect(config.locale).toBe("en");
  });

  it("falls back to Spanish for an unsupported locale", async () => {
    const config = await requestConfig({ requestLocale: Promise.resolve("fr") });
    expect(config.locale).toBe("es");
  });

  it("falls back to Spanish when locale is undefined", async () => {
    const config = await requestConfig({ requestLocale: Promise.resolve(undefined) });
    expect(config.locale).toBe("es");
  });
});
