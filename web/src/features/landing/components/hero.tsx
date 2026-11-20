import Link from "next/link";
import { Button, buttonVariants } from "@/components/ui/button";

export function Hero() {
  return (
    <section className="relative overflow-hidden">
      <div className="absolute inset-0 -z-10 bg-[radial-gradient(ellipse_at_top,var(--brand-cyan)/15%,transparent_60%)]" />
      <header className="mx-auto flex max-w-7xl items-center justify-between px-6 py-6">
        <div className="flex items-center gap-3">
          <img src="/brand/isotipo.png" alt="GELYX Labs" className="h-9 w-9" />
          <span className="font-heading text-xl font-bold tracking-tight">GELYX Labs</span>
        </div>
        <nav className="hidden items-center gap-8 text-sm md:flex">
          <a href="#servicios" className="text-muted-foreground hover:text-foreground">Servicios</a>
          <a href="#portafolio" className="text-muted-foreground hover:text-foreground">Portafolio</a>
          <a href="#proceso" className="text-muted-foreground hover:text-foreground">Proceso</a>
          <a href="#cotizacion" className="text-muted-foreground hover:text-foreground">Cotización</a>
        </nav>
        <div className="flex items-center gap-3">
          <Link href="/login" className={buttonVariants({ variant: "outline", size: "sm" })}>Acceder</Link>
          <a href="#cotizacion" className={buttonVariants({ size: "sm" })}>Cotizar proyecto</a>
        </div>
      </header>

      <div className="mx-auto max-w-7xl px-6 pb-24 pt-16 md:pt-24">
        <p className="mb-4 inline-flex items-center rounded-full border border-border bg-card px-4 py-1.5 text-xs font-medium text-muted-foreground">
          Technology & Innovation · Ideas into reality
        </p>
        <h1 className="font-heading max-w-4xl text-4xl font-bold leading-tight tracking-tight md:text-6xl">
          Convertimos tus ideas en{" "}
          <span className="bg-[var(--brand-gradient)] bg-clip-text text-transparent">
            soluciones digitales
          </span>{" "}
          que hacen crecer tu negocio.
        </h1>
        <p className="mt-6 max-w-2xl text-lg text-muted-foreground">
          En GELYX Labs diseñamos, desarrollamos e implementamos software a la medida para
          PYMES: consultoría, desarrollo web, datos e inteligencia artificial. Hacemos
          simple lo complejo.
        </p>
        <div className="mt-10 flex flex-wrap gap-4">
          <a href="#cotizacion" className={buttonVariants({ size: "lg" })}>Solicitar cotización</a>
          <a href="#portafolio" className={buttonVariants({ variant: "outline", size: "lg" })}>Ver portafolio</a>
        </div>
        <div className="mt-16 grid grid-cols-2 gap-6 text-sm text-muted-foreground md:grid-cols-4">
          <div><span className="font-heading text-2xl font-bold text-foreground">4</span><br />líneas de negocio</div>
          <div><span className="font-heading text-2xl font-bold text-foreground">4+</span><br />productos propios</div>
          <div><span className="font-heading text-2xl font-bold text-foreground">100%</span><br />enfoque práctico</div>
          <div><span className="font-heading text-2xl font-bold text-foreground">MX</span><br />con proyección global</div>
        </div>
      </div>
    </section>
  );
}