import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";

afterEach(() => {
  cleanup();
});

Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

vi.mock("next-intl", async () => {
  const actual = await vi.importActual<typeof import("next-intl")>("next-intl");
  const { default: messages } = await import("../../messages/es.json");

  function lookup(namespace: string | undefined, key: string): unknown {
    const path = [...(namespace?.split(".") ?? []), ...key.split(".")];
    let value: unknown = messages;
    for (const part of path) {
      if (typeof value !== "object" || value === null || !(part in value)) return undefined;
      value = (value as Record<string, unknown>)[part];
    }
    return value;
  }

  return {
    ...actual,
    useLocale: () => "es",
    useTranslations: (namespace?: string) => {
      const translate = (key: string, values?: Record<string, string | number>) => {
        const message = lookup(namespace, key);
        if (typeof message !== "string") return key;
        return message.replace(/\{(\w+)\}/g, (_match, name: string) => String(values?.[name] ?? `{${name}}`));
      };
      translate.has = (key: string) => typeof lookup(namespace, key) === "string";
      return translate;
    },
  };
});
