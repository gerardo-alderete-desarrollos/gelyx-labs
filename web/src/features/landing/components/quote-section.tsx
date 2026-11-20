import { QuoteForm } from "./quote-form";

export function QuoteSection() {
  return (
    <section id="cotizacion" className="py-24">
      <div className="mx-auto max-w-7xl px-6">
        <div className="grid gap-12 lg:grid-cols-2">
          <div>
            <h2 className="font-heading text-3xl font-bold tracking-tight md:text-4xl">Solicita una cotización</h2>
            <p className="mt-4 text-muted-foreground">
              Cuéntanos tu proyecto y te responderemos con una propuesta a medida. Sin compromiso.
            </p>
            <ul className="mt-8 space-y-3 text-sm text-muted-foreground">
              <li>✓ Respuesta en menos de 48 horas hábiles</li>
              <li>✓ Diagnóstico inicial sin costo</li>
              <li>✓ Propuesta clara y sin sobredimensionar</li>
            </ul>
          </div>
          <QuoteForm />
        </div>
      </div>
    </section>
  );
}