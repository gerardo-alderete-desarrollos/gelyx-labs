"use client";

import { useEffect, useState } from "react";

export function MagneticCursor() {
  const [pos, setPos] = useState({ x: -100, y: -100 });
  const [active, setActive] = useState(false);

  useEffect(() => {
    if (!window.matchMedia("(pointer: fine)").matches) return;
    let raf = 0;
    const target = { x: 0, y: 0 };
    const current = { x: 0, y: 0 };
    const onMove = (e: MouseEvent) => {
      target.x = e.clientX;
      target.y = e.clientY;
    };
    const onOver = (e: MouseEvent) => {
      const t = e.target as HTMLElement | null;
      setActive(!!t?.closest("a,button,[data-cursor]"));
    };
    window.addEventListener("mousemove", onMove);
    window.addEventListener("mouseover", onOver);
    const loop = () => {
      current.x += (target.x - current.x) * 0.18;
      current.y += (target.y - current.y) * 0.18;
      setPos({ x: current.x, y: current.y });
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => {
      cancelAnimationFrame(raf);
      window.removeEventListener("mousemove", onMove);
      window.removeEventListener("mouseover", onOver);
    };
  }, []);

  return (
    <div
      className="pointer-events-none fixed z-[95] hidden md:block"
      style={{ left: pos.x, top: pos.y, transform: "translate(-50%,-50%)" }}
    >
      <div
        className={`rounded-full border border-[var(--brand-cyan)] bg-[var(--brand-cyan)]/10 transition-all duration-200 ${
          active ? "h-12 w-12" : "h-4 w-4"
        }`}
      />
    </div>
  );
}