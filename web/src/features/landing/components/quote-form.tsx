"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { notify } from "@/lib/notify";

const schema = z.object({
  name: z.string().min(2, "Nombre requerido"),
  email: z.string().email("Correo inválido"),
  phone: z.string().optional(),
  company: z.string().optional(),
  service: z.string().optional(),
  budget: z.string().optional(),
  message: z.string().min(10, "Cuéntanos un poco más"),
});

type FormValues = z.infer<typeof schema>;

export function QuoteForm() {
  const [pending, setPending] = useState(false);
  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { name: "", email: "", message: "" } });

  async function onSubmit(values: FormValues) {
    setPending(true);
    try {
      const res = await fetch("/api/leads", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(values),
      });
      if (!res.ok) throw new Error("request_failed");
      notify.success("¡Gracias! Te contactaremos pronto.");
      form.reset();
    } catch {
      notify.error("No se pudo enviar la solicitud. Intenta de nuevo.");
    } finally {
      setPending(false);
    }
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4 rounded-2xl border border-border bg-card p-6">
      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <Input placeholder="Nombre" {...form.register("name")} />
          {form.formState.errors.name && <p className="mt-1 text-xs text-destructive">{form.formState.errors.name.message}</p>}
        </div>
        <div>
          <Input placeholder="Correo" type="email" {...form.register("email")} />
          {form.formState.errors.email && <p className="mt-1 text-xs text-destructive">{form.formState.errors.email.message}</p>}
        </div>
      </div>
      <div className="grid gap-4 md:grid-cols-2">
        <Input placeholder="Teléfono (opcional)" {...form.register("phone")} />
        <Input placeholder="Empresa (opcional)" {...form.register("company")} />
      </div>
      <div className="grid gap-4 md:grid-cols-2">
        <Input placeholder="Servicio de interés" {...form.register("service")} />
        <Input placeholder="Presupuesto estimado" {...form.register("budget")} />
      </div>
      <div>
        <textarea
          placeholder="Cuéntanos tu proyecto"
          rows={5}
          className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-xs placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          {...form.register("message")}
        />
        {form.formState.errors.message && <p className="mt-1 text-xs text-destructive">{form.formState.errors.message.message}</p>}
      </div>
      <Button type="submit" disabled={pending} className="w-full">
        {pending ? "Enviando..." : "Enviar solicitud"}
      </Button>
    </form>
  );
}