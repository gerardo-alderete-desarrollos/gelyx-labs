import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { LoadingOverlay } from "@/components/ui/loading-overlay";
import { loadingStore } from "@/lib/loading/loading-store";
import { act, render } from "@testing-library/react";

function renderOverlay() {
  return render(
    <>
      <div id="app-content">contenido</div>
      <LoadingOverlay />
    </>,
  );
}

describe("LoadingOverlay", () => {
  beforeEach(() => {
    loadingStore.reset();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  describe("visibility delay", () => {
    it("no aparece antes de 300 ms", () => {
      loadingStore.begin(new AbortController());
      const { container } = renderOverlay();
      expect(container.querySelector(".fixed")).toBeNull();
      act(() => {
        vi.advanceTimersByTime(299);
      });
      expect(container.querySelector(".fixed")).toBeNull();
      act(() => {
        vi.advanceTimersByTime(1);
      });
      expect(container.querySelector(".fixed")).not.toBeNull();
    });

    it("se mantiene al menos 400 ms visible", () => {
      const id = loadingStore.begin(new AbortController());
      const { container } = renderOverlay();
      act(() => {
        vi.advanceTimersByTime(300);
      });
      expect(container.querySelector(".fixed")).not.toBeNull();

      act(() => {
        loadingStore.end(id);
      });
      act(() => {
        vi.advanceTimersByTime(399);
      });
      expect(container.querySelector(".fixed")).not.toBeNull();
      act(() => {
        vi.advanceTimersByTime(1);
      });
      expect(container.querySelector(".fixed")).toBeNull();
    });
  });

  describe("inert attribute", () => {
    it("aplica inert al contenido cuando el overlay es visible", () => {
      loadingStore.begin(new AbortController());
      const { container } = renderOverlay();
      act(() => {
        vi.advanceTimersByTime(300);
      });
      const content = container.querySelector("#app-content");
      expect(content?.hasAttribute("inert")).toBe(true);
    });

    it("quita inert al ocultarse", () => {
      const id = loadingStore.begin(new AbortController());
      const { container } = renderOverlay();
      act(() => {
        vi.advanceTimersByTime(300);
      });
      act(() => {
        loadingStore.end(id);
      });
      act(() => {
        vi.advanceTimersByTime(400);
      });
      const content = container.querySelector("#app-content");
      expect(content?.hasAttribute("inert")).toBe(false);
    });
  });

  describe("focus restoration", () => {
    it("restaura el foco al elemento anterior al ocultarse", () => {
      const outside = document.createElement("button");
      outside.textContent = "fuera";
      document.body.appendChild(outside);
      outside.focus();

      const id = loadingStore.begin(new AbortController());
      const { container } = renderOverlay();
      act(() => {
        vi.advanceTimersByTime(300);
      });
      expect(document.activeElement).toBe(container.querySelector(".fixed"));

      act(() => {
        loadingStore.end(id);
      });
      act(() => {
        vi.advanceTimersByTime(400);
      });
      expect(document.activeElement).toBe(outside);
      outside.remove();
    });
  });

  describe("cancel button", () => {
    it("muestra botón Cancelar después de 10 segundos", () => {
      loadingStore.begin(new AbortController());
      const { container } = renderOverlay();
      act(() => {
        vi.advanceTimersByTime(300);
      });
      expect(container.querySelector("button")).toBeNull();
      act(() => {
        vi.advanceTimersByTime(10_000);
      });
      expect(container.querySelector("button")).not.toBeNull();
    });

    it("aborta las peticiones al hacer clic en Cancelar", () => {
      loadingStore.begin(new AbortController());
      loadingStore.begin(new AbortController());
      const { container } = renderOverlay();
      act(() => {
        vi.advanceTimersByTime(300);
      });
      act(() => {
        vi.advanceTimersByTime(10_000);
      });
      const cancelButton = container.querySelector("button");
      expect(cancelButton).not.toBeNull();
      act(() => {
        cancelButton?.click();
      });
      expect(loadingStore.getSnapshot()).toBe(0);
    });
  });

  describe("reduced motion", () => {
    it("usa pulso en lugar de animación al girar", () => {
      vi.spyOn(window, "matchMedia").mockReturnValue({
        matches: true,
        media: "(prefers-reduced-motion: reduce)",
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      });

      loadingStore.begin(new AbortController());
      const { container } = renderOverlay();
      act(() => {
        vi.advanceTimersByTime(300);
      });
      expect(container.querySelector(".animate-pulse")).not.toBeNull();
      expect(container.querySelector(".animate-spin")).toBeNull();
    });
  });
});
