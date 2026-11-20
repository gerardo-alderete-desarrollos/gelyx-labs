"use client";

import { useEffect, useState } from "react";

export function AnimatedHeadline({ text }: { text: string }) {
  const [show, setShow] = useState(false);
  useEffect(() => {
    const t = setTimeout(() => setShow(true), 120);
    return () => clearTimeout(t);
  }, []);
  const words = text.split(" ");
  return (
    <span>
      {words.map((w, i) => (
        <span
          key={i}
          className="inline-block"
          style={{
            opacity: show ? 1 : 0,
            transform: show ? "translateY(0)" : "translateY(0.6em)",
            transition: `opacity 700ms ease ${i * 90}ms, transform 700ms ease ${i * 90}ms`,
          }}
        >
          {w}&nbsp;
        </span>
      ))}
    </span>
  );
}