"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { fetchBff } from "@/lib/api/browser-client";
import { notify } from "@/lib/notify";

type ExportCsvButtonProps = Readonly<{
  path: string;
  label: string;
  loadingLabel: string;
  errorMessage: string;
  fallbackFileName: string;
}>;

function fileNameFromHeader(header: string | null, fallback: string) {
  const match = header?.match(/filename\*=UTF-8''([^;]+)/i);
  return match ? decodeURIComponent(match[1]) : fallback;
}

export function ExportCsvButton({
  path,
  label,
  loadingLabel,
  errorMessage,
  fallbackFileName,
}: ExportCsvButtonProps) {
  const [exporting, setExporting] = useState(false);

  const exportCsv = async () => {
    setExporting(true);
    try {
      const response = await fetchBff(path);
      if (!response.ok) {
        throw new Error("csv_export_failed");
      }

      const url = URL.createObjectURL(await response.blob());
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileNameFromHeader(response.headers.get("content-disposition"), fallbackFileName);
      document.body.append(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch {
      notify.error(errorMessage);
    } finally {
      setExporting(false);
    }
  };

  return <Button variant="outline" onClick={() => void exportCsv()} disabled={exporting}>{exporting ? loadingLabel : label}</Button>;
}
