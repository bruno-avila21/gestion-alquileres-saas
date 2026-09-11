#!/usr/bin/env python3
"""
Siembra una cartera de contratos creíble para mostrar el panel.

El demo tenía 85 propiedades en el sitio público y **un** contrato en el panel: la
pantalla de Contratos era una fila sola en una tabla vacía y el tablero mostraba
todo en 1. No es un problema de diseño, es de datos, y es lo primero que nota
alguien a quien le estás vendiendo la administración de su cartera.

Siembra propietarios, inquilinos y contratos sobre las propiedades que ya están
cargadas, cubriendo a propósito todo el abanico que el producto sabe hacer:
- los cuatro tipos de ajuste (ICL, IPC, porcentaje fijo, manual);
- cuatro frecuencias distintas (trimestral, cuatrimestral, semestral, anual);
- contratos con y sin punitorio, con distintas tolerancias;
- dos que vencen dentro de los 30 días, para que ese indicador no sea siempre 0.

Todo entra por la API: respeta el filtro multi-tenant, los validadores y las
reglas de dominio. Es idempotente por DNI de inquilino — volver a correrlo no
duplica nada.

Uso:
    python sembrar.py --api https://…/api/v1 --org palavecino --email … --password …
    python sembrar.py … --aplicar        # sin esto sólo muestra qué haría
"""
from __future__ import annotations

import argparse
import json
import sys
import urllib.error
import urllib.request
from datetime import date, timedelta
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


HOY = date.today()


def meses(n: int) -> timedelta:
    """Aproximación suficiente para fechas de contrato de demo."""
    return timedelta(days=int(n * 30.44))


PROPIETARIOS = [
    ("Marta Beatriz Ibarra", "27-14238907-4", "mibarra@demo.test", "011 4451-2288", "0720099388000012345671"),
    ("Estudio Roldán y Asoc.", "30-71223344-9", "admin@roldanasoc.demo.test", "011 4740-5566", "0170099220000098765432"),
    ("Hugo Nazareno Petrini", "20-10998877-1", "hpetrini@demo.test", "02320 45-8890", "0290099410000011223344"),
    ("Sucesión Vidal Aguirre", "30-69887766-2", "sucesion.vidal@demo.test", "011 4666-3311", "0110099530000055667788"),
]

# (nombre, apellido, dni, email, teléfono)
INQUILINOS = [
    ("Martín", "Sosa", "31447902", "msosa@demo.test", "11 6234-8890"),
    ("Lucía", "Benítez", "35128740", "lbenitez@demo.test", "11 5987-2210"),
    ("Diego", "Ferreyra", "29876154", "dferreyra@demo.test", "11 3345-7781"),
    ("Sofía", "Quiroga", "38214665", "squiroga@demo.test", "11 6712-0043"),
    ("Javier", "Ledesma", "26550318", "jledesma@demo.test", "11 4408-9925"),
    ("Carla", "Moyano", "33902187", "cmoyano@demo.test", "11 5561-3308"),
    ("Nicolás", "Arce", "30117549", "narce@demo.test", "11 6890-4417"),
    ("Valeria", "Ojeda", "36741022", "vojeda@demo.test", "11 3327-6654"),
    ("Ramiro", "Paz", "28093761", "rpaz@demo.test", "11 5104-2287"),
]

# dni, alquiler, ajuste, frecuencia, %, tasa punitoria diaria, gracia, día de cobro,
# meses ya transcurridos, meses hasta el vencimiento, depósito, nota, días hasta el
# vencimiento (opcional; pisa a los meses, para los contratos que vencen pronto)
CONTRATOS = [
    ("31447902",  520000, "ICL",          "FourMonthly",  None, 0.1,  5,  10, 8,  16, 520000, None, None),
    ("35128740",  690000, "ICL",          "SemiAnnual",   None, 0.15, 3,   5, 14, 10, 690000, None, None),
    ("29876154",  445000, "IPC",          "Quarterly",    None, None, 0,   1, 5,  19, 445000, None, None),
    ("38214665",  610000, "IPC",          "FourMonthly",  None, 0.1,  10, 15, 11, 13, 610000, None, None),
    ("26550318",  380000, "FixedPercent", "Quarterly",    8.0,  0.2,  0,  10, 6,  18, 380000, None, None),
    ("33902187",  875000, "FixedPercent", "SemiAnnual",   12.0, None, 0,   5, 20, 16, 875000, None, None),
    # Los dos que vencen dentro de los 30 días: sin ellos el indicador del tablero es siempre 0.
    ("30117549", 1150000, "ICL",          "Annual",       None, 0.1,  5,   1, 24, 0,  1150000,
     "Renovación en trámite. El propietario pidió pasar a ajuste cuatrimestral.", 12),
    ("36741022",  495000, "IPC",          "Quarterly",    None, 0.1,  5,  10, 22, 1,  495000,
     "El inquilino avisó que no renueva. Coordinar inspección de salida.", 26),
    ("28093761",  720000, "Manual",       "Quarterly",    None, None, 0,  10, 9,  15, 720000,
     "Ajuste pactado fuera de índice: se acuerda por escrito en cada período.", None),
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
    escribir = args.aplicar

    auth = http("POST", f"{base}/auth/login", body={
        "organizationSlug": args.org, "email": args.email, "password": args.password,
    })
    token = auth["token"]
    print(f"Autenticado en {auth['organizationSlug']}\n")

    contratos_previos = http("GET", f"{base}/contracts", token=token)
    props = http("GET", f"{base}/properties", token=token)
    inquilinos_previos = http("GET", f"{base}/tenants", token=token)
    print(f"Estado actual: {len(contratos_previos)} contratos, "
          f"{len(inquilinos_previos)} inquilinos, {len(props)} propiedades")

    # Propiedades libres: las que no tienen ya un contrato encima.
    ocupadas = {c["propertyId"] for c in contratos_previos}
    libres = [p for p in props if p["id"] not in ocupadas and p["isActive"]]
    # Se prefieren viviendas: un contrato de locación sobre un lote no se entiende en la demo.
    libres.sort(key=lambda p: (p["propertyType"] not in ("House", "Apartment", "PH"), p["address"]))
    if len(libres) < len(CONTRATOS):
        raise SystemExit(f"Sólo hay {len(libres)} propiedades libres para {len(CONTRATOS)} contratos.")

    por_dni = {t["dni"]: t for t in inquilinos_previos}
    faltan_inq = [i for i in INQUILINOS if i[2] not in por_dni]
    print(f"Propietarios a crear: {len(PROPIETARIOS)}")
    print(f"Inquilinos a crear:   {len(faltan_inq)} (de {len(INQUILINOS)}; el resto ya existe)")
    print(f"Contratos a crear:    {len(CONTRATOS)}\n")

    for (dni, renta, tipo, frec, pct, tasa, gracia, dia, desde, hasta, dep, nota, dias), prop in zip(CONTRATOS, libres):
        ini = HOY - meses(desde)
        fin = HOY + (timedelta(days=dias) if dias else meses(hasta))
        inq = next(i for i in INQUILINOS if i[2] == dni)
        punit = f"{tasa}%/día tras {gracia}d" if tasa else "sin punitorio"
        extra = f" · {pct}%" if pct else ""
        vence = " ← VENCE EN 30 DÍAS" if (fin - HOY).days <= 30 else ""
        print(f"  {inq[0]} {inq[1]:<9} ${renta:>9,} {tipo:<12}{extra:<7} {frec:<12} "
              f"{punit:<22} {ini}→{fin}{vence}")
        print(f"      {prop['address']}, {prop['city']}")

    if not escribir:
        print("\nSimulacro. Volvé a correrlo con --aplicar para escribir.")
        return 0

    print("\nEscribiendo…")
    propietarios = []
    for nombre, cuit, mail, tel, cbu in PROPIETARIOS:
        o = http("POST", f"{base}/owners", token=token, body={
            "name": nombre, "taxId": cuit, "email": mail, "phone": tel, "cbu": cbu, "notes": None,
        })
        propietarios.append(o)
        print(f"  ✓ propietario {nombre}")

    for nombre, apellido, dni, mail, tel in faltan_inq:
        t = http("POST", f"{base}/tenants", token=token, body={
            "firstName": nombre, "lastName": apellido, "dni": dni, "email": mail, "phone": tel,
        })
        por_dni[dni] = t
        print(f"  ✓ inquilino {nombre} {apellido}")

    for idx, ((dni, renta, tipo, frec, pct, tasa, gracia, dia, desde, hasta, dep, nota, dias), prop) in enumerate(
            zip(CONTRATOS, libres)):
        # El propietario se reparte entre los creados: así las rendiciones tienen a quién rendirle.
        dueno = propietarios[idx % len(propietarios)]
        http("PUT", f"{base}/properties/{prop['id']}", token=token, body={
            "address": prop["address"], "city": prop["city"], "province": prop["province"],
            "propertyType": prop["propertyType"], "areaM2": prop["areaM2"], "notes": prop["notes"],
            "ownerId": dueno["id"], "commissionPct": 8, "isActive": True,
            "details": {
                "neighborhood": prop["neighborhood"], "code": prop["code"],
                "description": prop["description"], "rooms": prop["rooms"],
                "bedrooms": prop["bedrooms"], "bathrooms": prop["bathrooms"],
                "garages": prop["garages"], "ageYears": prop["ageYears"],
                "coveredAreaM2": prop["coveredAreaM2"], "latitude": prop["latitude"],
                "longitude": prop["longitude"], "suitableForCredit": prop["suitableForCredit"],
                "features": prop["features"],
            },
        })
        c = http("POST", f"{base}/contracts", token=token, body={
            "propertyId": prop["id"],
            "appTenantId": por_dni[dni]["id"],
            "startDate": (HOY - meses(desde)).isoformat(),
            "endDate": (HOY + (timedelta(days=dias) if dias else meses(hasta))).isoformat(),
            "monthlyRent": renta,
            "currency": "ARS",
            "adjustmentType": tipo,
            "adjustmentFrequency": frec,
            "adjustmentPercent": pct,
            "lateFeeDailyRate": tasa,
            "lateFeeGraceDays": gracia,
            "dayOfMonth": dia,
            "depositAmount": dep,
            "notes": nota,
        })
        print(f"  ✓ contrato {c['appTenantFullName']} — {prop['address']}")

    print(f"\nListo: {len(PROPIETARIOS)} propietarios, {len(faltan_inq)} inquilinos, "
          f"{len(CONTRATOS)} contratos.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
