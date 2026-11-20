const techs = ["Next.js", "React", "TypeScript", ".NET", "PostgreSQL", "Tailwind", "Node.js", "Docker", "Vercel", "Neon", "OpenAI", "n8n"];

export function Marquee() {
  return (
    <div className="overflow-hidden border-y border-border bg-card/40 py-6">
      <div className="flex w-max animate-[marquee_30s_linear_infinite] gap-12 px-6">
        {[...techs, ...techs].map((t, i) => (
          <span key={i} className="font-heading text-lg font-semibold text-muted-foreground/70">
            {t}
          </span>
        ))}
      </div>
      <style>{`@keyframes marquee { from { transform: translateX(0); } to { transform: translateX(-50%); } }`}</style>
    </div>
  );
}