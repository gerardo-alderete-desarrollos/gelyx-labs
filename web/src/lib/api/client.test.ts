import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { fetchBff } from "@/lib/api/browser-client";
import { loadingStore } from "@/lib/loading/loading-store";

const fetchMock = vi.fn();

function pendingUntilAborted(init?: RequestInit): Promise<Response> {
  return new Promise<Response>((_resolve, reject) => {
    const signal = init?.signal;
    if (signal?.aborted) {
      reject(new DOMException("Aborted", "AbortError"));
      return;
    }
    signal?.addEventListener("abort", () => reject(new DOMException("Aborted", "AbortError")), { once: true });
  });
}

describe("fetchBff", () => {
  beforeEach(() => {
    loadingStore.reset();
    fetchMock.mockReset();
    fetchMock.mockImplementation((_input: RequestInfo | URL, init?: RequestInit) => pendingUntilAborted(init));
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  describe("silent option", () => {
    it("GET requests son silent por defecto", () => {
      const controller = new AbortController();
      const promise = fetchBff("/api/health", { signal: controller.signal });
      expect(loadingStore.getSnapshot()).toBe(0);
      controller.abort();
      return promise.then(
        () => {},
        () => expect(loadingStore.getSnapshot()).toBe(0),
      );
    });

    it("POST requests no son silent por defecto", () => {
      const controller = new AbortController();
      const promise = fetchBff("/api/test", {
        method: "POST",
        body: JSON.stringify({}),
        signal: controller.signal,
      });
      expect(loadingStore.getSnapshot()).toBe(1);
      controller.abort();
      return promise.then(
        () => {},
        () => expect(loadingStore.getSnapshot()).toBe(0),
      );
    });

    it("silent:true no incrementa el contador", () => {
      const controller = new AbortController();
      const promise = fetchBff("/api/test", {
        method: "POST",
        body: JSON.stringify({}),
        signal: controller.signal,
        silent: true,
      });
      expect(loadingStore.getSnapshot()).toBe(0);
      controller.abort();
      return promise.then(
        () => {},
        () => expect(loadingStore.getSnapshot()).toBe(0),
      );
    });
  });

  describe("timeout", () => {
    it("libera el bloqueo y notifica error al vencer el timeout", () => {
      const controller = new AbortController();
      const promise = fetchBff("/api/slow", {
        method: "POST",
        body: JSON.stringify({}),
        signal: controller.signal,
        timeoutMs: 50,
      });
      return promise.then(
        () => {
          throw new Error("debería haber fallado");
        },
        () => expect(loadingStore.getSnapshot()).toBe(0),
      );
    });
  });

  describe("duplicados idénticos", () => {
    it("mismetas idénticas comparten la misma promesa", () => {
      const promise1 = fetchBff("/api/test", { method: "GET" });
      const promise2 = fetchBff("/api/test", { method: "GET" });
      expect(promise1).toBe(promise2);
    });

    it("mutaciones diferentes no se deduplican", () => {
      const promise1 = fetchBff("/api/test", { method: "GET" });
      const promise2 = fetchBff("/api/test", { method: "POST" });
      expect(promise1).not.toBe(promise2);
    });
  });

  describe("401 refresh integration", () => {
    it("dispatches session-expired event on 401", async () => {
      const dispatchSpy = vi.spyOn(window, "dispatchEvent");
      fetchMock.mockResolvedValueOnce(new Response(null, { status: 401 }));
      const response = await fetchBff("/api/auth/me", { method: "GET" });
      expect(response.status).toBe(401);
      expect(dispatchSpy).toHaveBeenCalledWith(
        expect.objectContaining({ type: "gelyx-labs:session-expired" }),
      );
    });
  });
});
