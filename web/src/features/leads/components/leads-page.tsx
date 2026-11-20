"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { notify } from "@/lib/notify";

type Lead = {
  id: string;
  name: string;
  email: string;
  phone?: string | null;
  company?: string | null;
  service?: string | null;
  budget?: string | null;
  message: string;
  status: string;
  notes?: string | null;
};

export function LeadsPage() {
  const [items, setItems] = useState<Lead[]>([]);
  const [selected, setSelected] = useState<Lead | null>(null);
  const [notes, setNotes] = useState("");
  const [status, setStatus] = useState("new");
  const [subject, setSubject] = useState("");
  const [body, setBody] = useState("");

  async function load() {
    const res = await fetch("/api/leads");
    if (res.ok) setItems(await res.json());
  }

  useEffect(() => {
    let active = true;
    fetch("/api/leads")
      .then((res) => (res.ok ? res.json() : []))
      .then((data) => { if (active) setItems(data); })
      .catch(() => {});
    return () => { active = false; };
  }, []);

  function open(lead: Lead) {
    setSelected(lead);
    setNotes(lead.notes ?? "");
    setStatus(lead.status);
    setSubject(`Cotización GELYX Labs - ${lead.service ?? "desarrollo"}`);
    setBody(`Hola ${lead.name},\n\nGracias por contactarnos. Adjuntamos nuestra propuesta.\n\nSaludos,\nGELYX Labs`);
  }

  async function save() {
    if (!selected) return;
    const res = await fetch(`/api/leads/${selected.id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ status, notes }),
    });
    if (res.ok) { notify.success("Lead actualizado"); load(); setSelected(null); }
    else notify.error("No se pudo actualizar");
  }

  async function respond(channel: "email" | "whatsapp") {
    if (!selected) return;
    if (channel === "whatsapp") {
      const phone = (selected.phone ?? "").replace(/\D/g, "");
      const text = encodeURIComponent(body);
      const url = phone ? `https://wa.me/${phone}?text=${text}` : `https://wa.me/?text=${text}`;
      window.open(url, "_blank");
      // mark responded
      await fetch(`/api/leads/${selected.id}/respond`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ channel: "whatsapp", subject, body }),
      });
      notify.success("WhatsApp abierto");
      load();
      return;
    }
    const res = await fetch(`/api/leads/${selected.id}/respond`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ channel: "email", subject, body }),
    });
    if (res.ok) { notify.success("Correo enviado"); load(); setSelected(null); }
    else notify.error("No se pudo enviar el correo");
  }

  return (
    <div className="space-y-6">
      <h1 className="font-heading text-2xl font-bold">Leads / Cotizaciones</h1>
      <div className="overflow-x-auto rounded-2xl border border-border bg-card">
        <table className="w-full text-sm">
          <thead className="border-b border-border bg-muted/40">
            <tr>
              <th className="px-4 py-3 text-left">Nombre</th>
              <th className="px-4 py-3 text-left">Correo</th>
              <th className="px-4 py-3 text-left">Servicio</th>
              <th className="px-4 py-3 text-left">Estado</th>
              <th className="px-4 py-3 text-left">Acciones</th>
            </tr>
          </thead>
          <tbody>
            {items.map((l) => (
              <tr key={l.id} className="border-b border-border/60">
                <td className="px-4 py-3">{l.name}</td>
                <td className="px-4 py-3">{l.email}</td>
                <td className="px-4 py-3">{l.service ?? "-"}</td>
                <td className="px-4 py-3">{l.status}</td>
                <td className="px-4 py-3">
                  <Button size="sm" variant="outline" onClick={() => open(l)}>Ver</Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {selected && (
        <div className="rounded-2xl border border-border bg-card p-6 space-y-4">
          <h2 className="font-heading text-lg font-semibold">{selected.name}</h2>
          <p className="text-sm text-muted-foreground">{selected.email} {selected.phone ? `· ${selected.phone}` : ""}</p>
          <p className="text-sm">{selected.message}</p>
          <div className="grid gap-3 md:grid-cols-2">
            <select className="rounded-md border border-input bg-background px-3 py-2 text-sm" value={status} onChange={(e) => setStatus(e.target.value)}>
              <option value="new">Nuevo</option>
              <option value="contacted">Contactado</option>
              <option value="quoted">Cotizado</option>
              <option value="responded">Respondido</option>
              <option value="closed">Cerrado</option>
            </select>
            <Input placeholder="Notas" value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>
          <Button onClick={save}>Guardar estado</Button>

          <div className="border-t border-border pt-4 space-y-3">
            <h3 className="font-heading text-sm font-semibold">Responder cotización</h3>
            <Input placeholder="Asunto" value={subject} onChange={(e) => setSubject(e.target.value)} />
            <textarea
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-xs"
              rows={5}
              value={body}
              onChange={(e) => setBody(e.target.value)}
            />
            <div className="flex gap-3">
              <Button onClick={() => respond("email")}>Enviar por correo</Button>
              <Button variant="outline" onClick={() => respond("whatsapp")}>Abrir WhatsApp</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}