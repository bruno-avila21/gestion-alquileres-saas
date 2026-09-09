#!/usr/bin/env python3
"""
Siembra el pipeline del CRM.

El tablero de leads está construido entero —etapas, arrastre entre columnas, ficha,
notas, motivo de pérdida— y vacío. Un Kanban con cinco columnas que dicen "Sin
consultas" se ve peor que no tenerlo: parece que la función no anda.

Las consultas del sitio se crean por el endpoint **público**, el mismo que usa el
formulario de la web, así que quedan con `source = Website` de verdad y no por un
campo forzado. Las cargadas por el equipo entran por el endpoint de admin, que las
marca `Manual`. Después cada una se mueve a su etapa y se le agregan las notas.

Idempotente por email del interesado: volver a correrlo no duplica.

Uso:
    python sembrar_leads.py --api https://…/api/v1 --org palavecino --email … --password …
    python sembrar_leads.py … --aplicar
"""
from __future__ import annotations

import argparse
import json
import sys
import urllib.error
import urllib.request
from typing import Any

for _stream in (sys.stdout, sys.stderr):
    reconfigure = getattr(_stream, "reconfigure", None)
    if reconfigure:
        reconfigure(encoding="utf-8", errors="replace")


def http(method: str, url: str, *, token: str | None = None, body: dict | None = None) -> Any:
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            raw = r.read()
            return json.loads(raw) if raw else None
    except urllib.error.HTTPError as e:
        detail = e.read().decode("utf-8", "replace")[:500]
        raise SystemExit(f"{method} {url} → HTTP {e.code}\n{detail}") from e


# nombre, email, teléfono, mensaje, origen, etapa final, notas, motivo de pérdida
LEADS = [
    # ── Nuevas: entraron y nadie las tocó todavía ──
    ("Gastón Ruiz Díaz", "gruizdiaz@demo.test", "11 6604-2231",
     "Hola, vi el departamento de Villa Pueyrredón. ¿Sigue disponible? Necesitaría mudarme antes de fin de mes.",
     "Website", "New", [], None),
    ("Mariana Colombo", "mcolombo@demo.test", "11 5578-9012",
     "Buenas tardes, quería consultar por la casa de Bella Vista. ¿Acepta mascotas? Tengo dos gatos.",
     "Website", "New", [], None),
    ("Federico Ayala", "fayala@demo.test", "11 3390-4456",
     "Consulta por el local comercial. ¿Cuál es el requisito de garantía para comercio?",
     "Website", "New", [], None),
    ("Estudio Bianchi & Asoc.", "contacto@bianchi.demo.test", "011 4788-1200",
     "Buscamos oficina de 60 a 90 m² en zona norte para nuestro estudio contable. Presupuesto hasta USD 1.200 mensuales.",
     "Manual", "New", [], None),

    # ── Contactadas: ya hubo un ida y vuelta ──
    ("Paula Recalde", "precalde@demo.test", "11 6712-3348",
     "Me interesa el PH de San Miguel. ¿Se puede visitar un sábado?",
     "Website", "Contacted", [
         "Llamada del martes: le mandé ficha completa y fotos. Le sirve el sábado a la mañana, queda en confirmar.",
     ], None),
    ("Hernán Vitali", "hvitali@demo.test", "11 4423-7789",
     "Busco casa con quincho en Jose Clemente Paz, hasta USD 160.000. Vendo mi departamento para comprar.",
     "Website", "Contacted", [
         "Tiene un depto en Devoto para vender primero. Le ofrecí tasarlo sin cargo, quedó en pensarlo.",
         "Insistí por WhatsApp. Contestó que la semana que viene se define.",
     ], None),
    ("Silvina Peralta", "speralta@demo.test", "11 5290-6614",
     "Consulta por alquiler en Muñiz. Somos dos, ambos en relación de dependencia, con recibo.",
     "Manual", "Contacted", [
         "Vino por recomendación de la inquilina de Andrade al 1500. Cumple los requisitos de garantía.",
     ], None),

    # ── Visita coordinada ──
    ("Rodrigo Santillán", "rsantillan@demo.test", "11 6845-0092",
     "Quiero ver la casa de Luján. ¿Tienen disponibilidad esta semana?",
     "Website", "Visit", [
         "Visita coordinada para el jueves 15:30 en Bv. Doctor Domingo Cabred. Confirmó por WhatsApp.",
         "Le avisé que las llaves las tiene el casero del fondo.",
     ], None),
    ("Ana Clara Méndez", "acmendez@demo.test", "11 3067-5521",
     "Vi el departamento de Caballito publicado. Somos familia con un nene. ¿Se puede visitar?",
     "Website", "Visit", [
         "Visita el viernes a las 11. Le interesa mucho por la escuela a dos cuadras.",
     ], None),
    ("Marcelo Ferrari", "mferrari@demo.test", "11 5511-2874",
     "Inversor. Busco una o dos unidades para renta en CABA.",
     "Manual", "Visit", [
         "Quiere ver las tres de Capital el mismo día. Coordinado para el miércoles desde las 14.",
     ], None),

    # ── En negociación ──
    ("Familia Quiroga Bustos", "quirogabustos@demo.test", "11 6390-4471",
     "Nos interesa la casa de San Miguel. Queremos hacer una oferta.",
     "Website", "Negotiation", [
         "Ofrecieron USD 275.000 sobre 299.000 publicados. Se lo trasladé al propietario.",
         "El propietario contraofertó 289.000. Están evaluando, piden hasta el lunes.",
     ], None),
    ("Lorena Grinberg", "lgrinberg@demo.test", "11 4802-3315",
     "Quiero alquilar el departamento de Palermo. Ya tengo la garantía propietaria lista.",
     "Manual", "Negotiation", [
         "Garantía verificada: propiedad en CABA libre de gravámenes. Falta que firme el propietario.",
     ], None),

    # ── Cerradas ──
    ("Nicolás Arce", "narce@demo.test", "11 6890-4417",
     "Consulta por el departamento de Av Quintana. Vengo de otra inmobiliaria que no me respondió.",
     "Website", "Won", [
         "Firmó contrato por dos años con ajuste ICL anual. Entregadas las llaves.",
     ], None),
    ("Damián Sotelo", "dsotelo@demo.test", "11 5744-9903",
     "Busco terreno en barrio cerrado zona norte, hasta USD 50.000.",
     "Website", "Lost", [
         "Le mostré las dos opciones de Maschwitz. Ninguna le cerró por la distancia al trabajo.",
     ], "Compró en otra zona (Pilar) por cercanía laboral."),
]


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--api", required=True)
    ap.add_argument("--org", required=True)
    ap.add_argument("--email", required=True)
    ap.add_argument("--password", required=True)
    ap.add_argument("--aplicar", action="store_true", help="Escribir (por defecto: simulacro)")
    args = ap.parse_args()

    base = args.api.rstrip("/")

    auth = http("POST", f"{base}/auth/login", body={
        "organizationSlug": args.org, "email": args.email, "password": args.password,
    })
    token = auth["token"]
    print(f"Autenticado en {auth['organizationSlug']}\n")

    previos = http("GET", f"{base}/leads?page=1&pageSize=500", token=token)
    existentes = {l.get("email") for l in previos["items"] if l.get("email")}
    faltan = [l for l in LEADS if l[1] not in existentes]

    # Publicaciones reales para colgarles la consulta: una consulta sin propiedad se ve
    # despegada del catálogo, que es justo lo que la demo quiere mostrar conectado.
    listings = http("GET", f"{base}/public/{args.org}/listings?pageSize=40")["items"]

    print(f"Consultas ya cargadas: {len(previos['items'])}")
    print(f"A crear: {len(faltan)} de {len(LEADS)}\n")

    if not faltan:
        print("Nada que sembrar: ya están todas.")
        return 0

    por_etapa: dict[str, int] = {}
    for nombre, mail, tel, msg, origen, etapa, notas, _ in faltan:
        por_etapa[etapa] = por_etapa.get(etapa, 0) + 1
        print(f"  {etapa:<12} {origen:<8} {nombre:<26} {len(notas)} nota(s)")
    print("\n  Por etapa: " + " · ".join(f"{k}={v}" for k, v in por_etapa.items()))

    if not args.aplicar:
        print("\nSimulacro. Volvé a correrlo con --aplicar para escribir.")
        return 0

    print()
    for idx, (nombre, mail, tel, msg, origen, etapa, notas, motivo) in enumerate(faltan):
        listing = listings[idx % len(listings)] if listings else None
        cuerpo = {"name": nombre, "email": mail, "phone": tel, "message": msg}
        if listing:
            cuerpo["listingId"] = listing["id"]

        if origen == "Website":
            # Mismo endpoint que el formulario del sitio: responde 204 sin cuerpo, así que
            # hay que buscar el lead recién creado para quedarse con su id.
            http("POST", f"{base}/public/{args.org}/leads", body=cuerpo)
            recientes = http("GET", f"{base}/leads?page=1&pageSize=500", token=token)["items"]
            creado = next((l for l in recientes if l.get("email") == mail), None)
            if creado is None:
                print(f"  ! {nombre}: no se pudo recuperar el lead recién creado, se saltea")
                continue
        else:
            creado = http("POST", f"{base}/leads", token=token, body=cuerpo)

        for texto in notas:
            http("POST", f"{base}/leads/{creado['id']}/notes", token=token, body={"text": texto})

        if etapa != "New":
            estado = {"status": etapa}
            if motivo:
                estado["lostReason"] = motivo
            http("PATCH", f"{base}/leads/{creado['id']}/status", token=token, body=estado)

        print(f"  ✓ {etapa:<12} {nombre}")

    print(f"\nListo: {len(faltan)} consultas sembradas.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
