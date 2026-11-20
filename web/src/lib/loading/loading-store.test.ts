import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { loadingStore } from "@/lib/loading/loading-store";

describe("loading-store", () => {
  beforeEach(() => {
    loadingStore.reset();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe("begin/end", () => {
    it("incrementa el contador al comenzar y lo decrementa al terminar", () => {
      const controller1 = new AbortController();
      const id1 = loadingStore.begin(controller1);
      expect(loadingStore.getSnapshot()).toBe(1);

      const controller2 = new AbortController();
      const id2 = loadingStore.begin(controller2);
      expect(loadingStore.getSnapshot()).toBe(2);

      loadingStore.end(id1);
      expect(loadingStore.getSnapshot()).toBe(1);

      loadingStore.end(id2);
      expect(loadingStore.getSnapshot()).toBe(0);
    });

    it("el contador nunca baja de 0 aunque se llame end sin begin", () => {
      loadingStore.end("non-existent-id");
      expect(loadingStore.getSnapshot()).toBe(0);
    });

    it("varios requests simultáneos", () => {
      const controllers = [new AbortController(), new AbortController(), new AbortController()];
      const ids = controllers.map((c) => loadingStore.begin(c));
      expect(loadingStore.getSnapshot()).toBe(3);

      loadingStore.end(ids[0]);
      expect(loadingStore.getSnapshot()).toBe(2);

      loadingStore.end(ids[1]);
      expect(loadingStore.getSnapshot()).toBe(1);

      loadingStore.end(ids[2]);
      expect(loadingStore.getSnapshot()).toBe(0);
    });
  });

  describe("cancelAll", () => {
    it("aborta todos los controladores y reinicia el contador", () => {
      const controller1 = new AbortController();
      const controller2 = new AbortController();
      loadingStore.begin(controller1);
      loadingStore.begin(controller2);
      expect(loadingStore.getSnapshot()).toBe(2);

      loadingStore.cancelAll();
      expect(loadingStore.getSnapshot()).toBe(0);
    });
  });
});