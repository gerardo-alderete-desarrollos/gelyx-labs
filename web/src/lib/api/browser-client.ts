"use client";

import { correlationHeader, createCorrelationId, isSafeCorrelationId } from "@/lib/correlation";
import { loadingStore } from "@/lib/loading/loading-store";
import { notify } from "@/lib/notify";

type ClientRequestOptions = RequestInit & {
  silent?: boolean;
  timeoutMs?: number;
};

const inFlight = new Map<string, Promise<Response>>();

function requestKey(path: string, init: ClientRequestOptions): string {
  return `${init.method?.toUpperCase() ?? "GET"} ${path} ${typeof init.body === "string" ? init.body : ""}`;
}

function isPublicAuthEndpoint(path: string): boolean {
  return ["/api/auth/login", "/api/auth/forgot-password", "/api/auth/reset-password", "/api/auth/2fa"].includes(path);
}

function isRefreshEndpoint(path: string): boolean {
  return ["/api/auth/refresh"].includes(path);
}

export function fetchBff(path: string, init: ClientRequestOptions = {}): Promise<Response> {
  const key = requestKey(path, init);
  const existing = inFlight.get(key);
  if (existing) return existing;

  const headers = new Headers(init.headers);
  const incomingCorrelationId = headers.get(correlationHeader);
  headers.set(
    correlationHeader,
    isSafeCorrelationId(incomingCorrelationId) ? incomingCorrelationId : createCorrelationId(),
  );
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const controller = new AbortController();
  const method = init.method?.toUpperCase() ?? "GET";
  const isMutation = ["POST", "PUT", "PATCH", "DELETE"].includes(method);
  const silent = init.silent ?? (!isMutation || isRefreshEndpoint(path));

  const timeout = window.setTimeout(() => {
    controller.abort();
    notify.error("request_timeout");
  }, init.timeoutMs ?? 30_000);

  const abort = () => controller.abort();
  init.signal?.addEventListener("abort", abort, { once: true });

  const requestId = silent ? null : loadingStore.begin(controller);

  const request = fetch(path, {
    ...init,
    headers,
    signal: controller.signal,
    credentials: "same-origin",
  })
    .then((response) => {
      if (response.status === 401 && !isPublicAuthEndpoint(path)) {
        window.dispatchEvent(new CustomEvent("gelyx-labs:session-expired"));
      }
      return response;
    })
    .finally(() => {
      window.clearTimeout(timeout);
      init.signal?.removeEventListener("abort", abort);
      if (requestId) loadingStore.end(requestId);
      inFlight.delete(key);
    });

  inFlight.set(key, request);
  return request;
}
