import { describe, expect, it } from "vitest";
import { correlationIdFromError, isSafeCorrelationId } from "@/lib/correlation";

describe("correlation IDs", () => {
  it("accepts only safe values", () => {
    expect(isSafeCorrelationId("web-request_123:attempt.1")).toBe(true);
    expect(isSafeCorrelationId("unsafe value\n")).toBe(false);
  });

  it("extracts a safe correlation ID from an error cause", () => {
    const error = new Error("Page rendering failed", {
      cause: { correlationId: "api-request-123" },
    });

    expect(correlationIdFromError(error)).toBe("api-request-123");
  });

  it("does not expose arbitrary error details as a correlation ID", () => {
    const error = new Error("database password leaked", {
      cause: "database password leaked",
    });

    expect(correlationIdFromError(error)).toBeUndefined();
  });
});
