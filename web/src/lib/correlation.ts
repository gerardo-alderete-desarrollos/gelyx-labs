export const correlationHeader = "X-Correlation-Id";

const correlationIdPattern = /^[A-Za-z0-9][A-Za-z0-9._:-]{0,127}$/;

export function isSafeCorrelationId(value: unknown): value is string {
  return typeof value === "string" && correlationIdPattern.test(value);
}

export function createCorrelationId(): string {
  return crypto.randomUUID().replaceAll("-", "");
}

export function correlationIdFromError(error: Error & { digest?: string }): string | undefined {
  const visited = new Set<unknown>();

  const find = (value: unknown, depth: number): string | undefined => {
    if (depth > 2 || value === null || typeof value !== "object" && typeof value !== "string" || visited.has(value)) {
      return undefined;
    }
    visited.add(value);

    if (typeof value === "string") {
      const match = /(?:correlationId|x-correlation-id)\s*[:=]\s*([A-Za-z0-9._:-]{1,128})/i.exec(value);
      return match && isSafeCorrelationId(match[1]) ? match[1] : undefined;
    }

    const candidate = value as { correlationId?: unknown; cause?: unknown; digest?: unknown };
    if (isSafeCorrelationId(candidate.correlationId)) {
      return candidate.correlationId;
    }

    return find(candidate.digest, depth + 1) ?? find(candidate.cause, depth + 1);
  };

  return find(error, 0);
}
