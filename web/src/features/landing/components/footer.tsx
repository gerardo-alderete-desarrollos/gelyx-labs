const socials = [
  { label: "LinkedIn", href: "https://www.linkedin.com/company/gelyx-labs" },
  { label: "Instagram", href: "https://www.instagram.com/gelyxlabs" },
  { label: "Facebook", href: "https://www.facebook.com/gelyxlabs" },
  { label: "X", href: "https://x.com/gelyxlabs" },
  { label: "TikTok", href: "https://www.tiktok.com/@gelyxlabs" },
  { label: "YouTube", href: "https://www.youtube.com/@gelyxlabs" },
];

export function SiteFooter() {
  return (
    <footer className="border-t border-border bg-[#10152D] text-white">
      <div className="mx-auto max-w-7xl px-6 py-20 text-center">
        <p className="text-sm uppercase tracking-widest text-white/60">¿Tienes un proyecto en mente?</p>
        <h3 className="mt-4 font-heading text-4xl font-bold md:text-6xl">Hagámoslo realidad.</h3>
        <a href="#cotizacion" className="mt-8 inline-block rounded-full bg-[var(--brand-gradient)] px-8 py-4 text-sm font-semibold text-white shadow-lg transition hover:scale-105">
          Solicitar cotización
        </a>
      </div>
      <div className="mx-auto max-w-7xl px-6 py-16">
        <div className="grid gap-10 md:grid-cols-4">
          <div className="md:col-span-2">
            <div className="flex items-center gap-3">
              <img src="/brand/logo-light.png" alt="GELYX Labs" className="h-8" />
            </div>
            <p className="mt-4 max-w-md text-sm text-white/70">
              GELYX Labs — Technology & Innovation. Ideas into reality. Hacemos simple lo complejo.
            </p>
          </div>
          <div>
            <h4 className="font-heading text-sm font-semibold">Servicios</h4>
            <ul className="mt-3 space-y-2 text-sm text-white/70">
              <li>Consulting</li>
              <li>Studio</li>
              <li>Data</li>
              <li>AI</li>
            </ul>
          </div>
          <div>
            <h4 className="font-heading text-sm font-semibold">Redes</h4>
            <ul className="mt-3 space-y-2 text-sm text-white/70">
              {socials.map((s) => (
                <li key={s.label}>
                  <a href={s.href} target="_blank" rel="noreferrer" className="hover:text-white">
                    {s.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>
        </div>
        <div className="mt-12 border-t border-white/10 pt-6 text-xs text-white/50">
          © {new Date().getFullYear()} GELYX Labs. Todos los derechos reservados.
        </div>
      </div>
    </footer>
  );
}