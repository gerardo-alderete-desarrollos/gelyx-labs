const clients = ["Acme", "Nebula", "Vertex", "Quantum", "Pulse", "Orbit", "Nova", "Apex"];

export function ClientLogos() {
  return (
    <section className="border-y border-border bg-card/30 py-10">
      <div className="mx-auto max-w-7xl px-6">
        <p className="mb-6 text-center text-xs uppercase tracking-widest text-muted-foreground">
          Empresas que confían en nosotros
        </p>
        <div className="flex flex-wrap items-center justify-center gap-x-12 gap-y-4">
          {clients.map((c) => (
            <span key={c} className="font-heading text-xl font-bold text-muted-foreground/50">
              {c}
            </span>
          ))}
        </div>
      </div>
    </section>
  );
}