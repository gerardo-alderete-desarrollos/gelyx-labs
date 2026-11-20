"use client";

import { useEffect, useState } from "react";

export function PageLoader() {
  const [show, setShow] = useState(true);
  useEffect(() => {
    if (sessionStorage.getItem("gelyx-loaded")) {
      setShow(false);
      return;
    }
    const t = setTimeout(() => {
      setShow(false);
      sessionStorage.setItem("gelyx-loaded", "1");
    }, 1100);
    return () => clearTimeout(t);
  }, []);
  if (!show) return null;
  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-background transition-opacity duration-500">
      <img src="/brand/isotipo.png" alt="GELYX Labs" className="h-16 w-16 animate-pulse" />
    </div>
  );
}