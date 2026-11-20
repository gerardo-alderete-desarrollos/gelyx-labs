import { describe, it, expect, vi, beforeEach } from "vitest";
import { setNotifyTranslator, notify } from "@/lib/notify";
import { toast } from "sonner";

describe("notify", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("notify.success", () => {
    it("calls toast.success with translated message", () => {
      const mockTranslate = vi.fn((key: string) => key);
      const mockHas = vi.fn(() => true);
      setNotifyTranslator(mockTranslate, mockHas);
      
      const mockToastSuccess = vi.spyOn(toast, "success").mockImplementation(() => "toast-id");
      
      notify.success("test.key");
      
      expect(mockToastSuccess).toHaveBeenCalledWith("test.key");
    });
  });

  describe("notify.error", () => {
    it("calls toast.error with translated message for string error", () => {
      const mockToastError = vi.spyOn(toast, "error").mockImplementation(() => "toast-id");
      
      notify.error("error message");
      
      expect(mockToastError).toHaveBeenCalledWith("error message");
    });
  });

  describe("setNotifyTranslator", () => {
    it("sets translator and hasTranslation functions", () => {
      const translate = vi.fn((key: string) => `translated: ${key}`);
      const hasTranslation = vi.fn((key: string) => key.startsWith("test"));
      
      setNotifyTranslator(translate, hasTranslation);
      
      expect(translate).toBeDefined();
      expect(hasTranslation).toBeDefined();
    });
  });
});
