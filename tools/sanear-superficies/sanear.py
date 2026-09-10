#!/usr/bin/env python3
"""
Corrige las superficies que el scraper de Tokko cargó multiplicadas por cien.

En la ficha de Tokko conviven dos formatos de número: el precio va en es-AR, con el
punto como separador de miles ("$ 800.000"), y las superficies van con el punto como
separador decimal ("86.59 m²", "19.7 m²"). `numero()` estaba escrito para el precio y
borraba todos los puntos, así que el terreno de un PH entraba a la base cien veces más
grande:

    terreno = "86.59 m²"   →   areaM2 = 8659      →  "Andrade al 1500 · 8.659 m²"

El scraper ya está arreglado (`tools/importar-sitio-tokko/scrape.py`), pero las 85
propiedades importadas antes del arreglo siguen sucias y la superficie se muestra en la
cartera y en el sitio público. Este script las corrige por API, que es el único camino
que respeta el filtro multi-tenant y las validaciones del dominio.

La verdad la pone `salida-palavecino/propiedades.json`, que guarda los atributos crudos
tal como los publicó Tokko: se vuelve a parsear el texto original con la función ya
corregida en vez de adivinar dónde iba la coma a partir del número roto.

Sólo toca una propiedad si el valor que tiene hoy en la base es exactamente el que
producía el parseo viejo. Si alguien la editó a mano, la deja como está y lo avisa. Eso
lo hace idempotente: correrlo dos veces no cambia nada la segunda vez.

Uso:
    python sanear.py --api https://…/api/v1 --org palavecino --email … --password …
    python sanear.py … --aplicar        # sin esto sólo muestra qué haría
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import sys
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any

# La consola de Windows arranca en cp1252 y se ahoga con las flechas y los acentos del
# informe. Forzar UTF-8 en la salida evita un UnicodeEncodeError a mitad del listado.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

RAIZ = Path(__file__).resolve().parents[1] / "importar-sitio-tokko"
SCRAPER = RAIZ / "scrape.py"
SALIDA = RAIZ / "salida-palavecino" / "propiedades.json"

# El orden de claves con el que el scraper resuelve cada superficie. Se replica acá en
# vez de importar la función entera porque `detalle()` necesita el HTML de la ficha.
CLAVES_TOTAL = ("superficie total", "terreno", "superficie del terreno", "superficie terreno", "total construido")
CLAVES_CUBIERTA = ("superficie cubierta", "cubierta")


def cargar_numero():
    """Trae `numero()` del scraper para no tener dos copias de la regla de parseo."""
    spec = importlib.util.spec_from_file_location("scrape_tokko", SCRAPER)
    if spec is None or spec.loader is None:
        raise SystemExit(f"No pude cargar {SCRAPER}")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod.numero


def numero_viejo(s: str | None) -> float | None:
    """El parseo con el error, para reconocer qué valores dejó y no pisar ediciones a mano."""
    import re

    if not s:
        return None
    m = re.search(r"-?\d[\d\.]*(?:,\d+)?", s)
    if not m:
        return None
    return float(m.group(0).replace(".", "").replace(",", "."))


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
        detail = e.read().decode("utf-8", "replace")[:400]
        raise SystemExit(f"{method} {url} → HTTP {e.code}\n{detail}") from e


def primer_atributo(atributos: dict, claves: tuple[str, ...]) -> str | None:
    for k in claves:
        if k in atributos:
            return atributos[k]
    return None


def igual(a: float | None, b: float | None) -> bool:
    if a is None or b is None:
        return a is None and b is None
    return abs(a - b) < 0.005


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--api", required=True, help="Base de la API, ej. https://…/api/v1")
    ap.add_argument("--org", required=True, help="Slug de la organización")
    ap.add_argument("--email", required=True)
    ap.add_argument("--password", required=True)
    ap.add_argument("--aplicar", action="store_true", help="Escribir los cambios (por defecto: simulacro)")
    args = ap.parse_args()

    if not SALIDA.exists():
        raise SystemExit(f"Falta {SALIDA}: sin los atributos crudos no hay con qué corregir.")

    numero = cargar_numero()
    scrapeadas = json.loads(SALIDA.read_text(encoding="utf-8"))

    # Lo que Tokko decía de verdad, por código de propiedad.
    verdad: dict[str, dict[str, float | None]] = {}
    for p in scrapeadas:
        code = p.get("code")
        atributos = p.get("atributos") or {}
        if not code or not atributos:
            continue
        crudo_total = primer_atributo(atributos, CLAVES_TOTAL)
        crudo_cub = primer_atributo(atributos, CLAVES_CUBIERTA)
        verdad[code] = {
            "areaM2": numero(crudo_total),
            "areaM2_viejo": numero_viejo(crudo_total),
            "coveredAreaM2": numero(crudo_cub),
            "coveredAreaM2_viejo": numero_viejo(crudo_cub),
        }

    base = args.api.rstrip("/")
    auth = http("POST", f"{base}/auth/login", body={
        "organizationSlug": args.org, "email": args.email, "password": args.password,
    })
    token = auth["token"]
    print(f"Autenticado como {auth['email']} en {auth['organizationSlug']}")

    props = http("GET", f"{base}/properties", token=token)
    print(f"{len(props)} propiedades en la cartera, {len(verdad)} en la importación\n")

    arreglar: list[tuple[dict, float | None, float | None]] = []
    a_mano: list[dict] = []
    sin_origen = 0

    for p in props:
        v = verdad.get(p.get("code") or "")
        if not v:
            sin_origen += 1
            continue

        nueva_total = p["areaM2"]
        nueva_cub = p["coveredAreaM2"]
        toca = False

        for campo, actual in (("areaM2", p["areaM2"]), ("coveredAreaM2", p["coveredAreaM2"])):
            correcto, roto = v[campo], v[f"{campo}_viejo"]
            if igual(correcto, roto) or igual(actual, correcto):
                continue  # nunca estuvo mal, o ya está corregido
            if not igual(actual, roto):
                a_mano.append(p)  # lo editaron después de importar: no lo pisamos
                break
            if campo == "areaM2":
                nueva_total = correcto
            else:
                nueva_cub = correcto
            toca = True
        else:
            if toca:
                arreglar.append((p, nueva_total, nueva_cub))

    if sin_origen:
        print(f"({sin_origen} propiedades sin origen en la importación: no se tocan)")
    for p in a_mano:
        print(f"  ! {p.get('code')}  {p['areaM2']} m² no coincide con lo importado: editada a mano, se deja")

    if not arreglar:
        print("\nNada que sanear: ninguna superficie quedó con el parseo viejo.")
        return 0

    print(f"\n{len(arreglar)} superficies cargadas cien veces más grandes:\n")
    for p, total, cub in arreglar:
        det = f"  {p.get('code') or p['id'][:8]}  {p['propertyType']:11} {p['address'][:32]:32}"
        if not igual(p["areaM2"], total):
            det += f"  total {p['areaM2']:g} → {total:g}"
        if not igual(p["coveredAreaM2"], cub):
            det += f"  cubierta {p['coveredAreaM2']:g} → {cub:g}"
        print(det)

    if not args.aplicar:
        print("\nSimulacro. Volvé a correrlo con --aplicar para escribir los cambios.")
        return 0

    print()
    for p, total, cub in arreglar:
        # El PUT reemplaza la propiedad entera: hay que reenviar la ficha pública completa
        # o se borra (Details va como null y el handler la limpia).
        http("PUT", f"{base}/properties/{p['id']}", token=token, body={
            "address": p["address"],
            "city": p["city"],
            "province": p["province"],
            "propertyType": p["propertyType"],
            "areaM2": total,
            "notes": p["notes"],
            "ownerId": p["ownerId"],
            "commissionPct": p["commissionPct"],
            "isActive": p["isActive"],
            "details": {
                "neighborhood": p["neighborhood"],
                "code": p["code"],
                "description": p["description"],
                "rooms": p["rooms"],
                "bedrooms": p["bedrooms"],
                "bathrooms": p["bathrooms"],
                "garages": p["garages"],
                "ageYears": p["ageYears"],
                "coveredAreaM2": cub,
                "latitude": p["latitude"],
                "longitude": p["longitude"],
                "suitableForCredit": p["suitableForCredit"],
                "features": p["features"],
            },
        })
        print(f"  ✓ {p.get('code') or p['id'][:8]}  →  {total:g} m²")

    print(f"\nListo: {len(arreglar)} propiedades corregidas.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
