import { describe, expect, it } from "vitest";
import english from "../../messages/en.json";
import spanish from "../../messages/es.json";

function messageKeys(value: unknown, prefix = ""): string[] {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    return [prefix];
  }

  return Object.entries(value).flatMap(([key, child]) =>
    messageKeys(child, prefix ? `${prefix}.${key}` : key),
  );
}

describe("message catalogs", () => {
  it("contains the same keys in English and Spanish", () => {
    expect(messageKeys(spanish).sort()).toEqual(messageKeys(english).sort());
  });
});
