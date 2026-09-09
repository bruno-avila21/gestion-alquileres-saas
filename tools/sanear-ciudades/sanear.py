#!/usr/bin/env python3
"""
Saca la dirección que quedó pegada al campo `city` de las propiedades importadas.

El scraper de Tokko comparaba el `alt` de la foto contra `address` sin normalizar
los espacios, y el `alt` trae espacios dobles donde `address` tiene uno solo
("Av Cordoba  al 5800" vs "Av Cordoba al 5800"). El endswith fallaba y la
dirección terminaba dentro de la ciudad:

    city    = "Capital Federal Av Cordoba  al 5800"
    address = "Av Cordoba al 5800"          →  city correcta: "Capital Federal"

El scraper ya está arreglado (`tools/importar-sitio-tokko/scrape.py`), pero las
propiedades cargadas antes del arreglo siguen sucias y se ven en todo el sitio
público. Este script las corrige por API, que es el único camino que respeta el
filtro multi-tenant y las validaciones del dominio.

Uso:
    python sanear.py --api https://…/api/v1 --org palavecino --email … --password …
    python sanear.py … --aplicar        # sin esto sólo muestra qué haría
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
from typing import Any

# La consola de Windows arranca en cp1252 y se ahoga con las flechas y los acentos
# del informe. Forzar UTF-8 en la salida evita un UnicodeEncodeError a mitad del listado.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")


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


def ciudad_limpia(city: str | None, address: str | None) -> str | None:
    """Devuelve la ciudad sin la dirección pegada, o None si no hacía falta tocarla."""
    if not city or not address:
        return None
    ciudad = re.sub(r"\s+", " ", city).strip()
    direccion = re.sub(r"\s+", " ", address).strip()
    if not direccion or not ciudad.endswith(direccion):
        return None
    limpia = ciudad[: -len(direccion)].strip(" ,-")
    # Si al sacar la dirección no queda ciudad, la original era sólo la dirección:
    # no hay nada que rescatar y es mejor dejarla como está que vaciar el campo.
    return limpia or None


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--api", required=True, help="Base de la API, ej. https://…/api/v1")
    ap.add_argument("--org", required=True, help="Slug de la organización")
    ap.add_argument("--email", required=True)
    ap.add_argument("--password", required=True)
    ap.add_argument("--aplicar", action="store_true", help="Escribir los cambios (por defecto: simulacro)")
    args = ap.parse_args()

    base = args.api.rstrip("/")

    auth = http("POST", f"{base}/auth/login", body={
        "organizationSlug": args.org, "email": args.email, "password": args.password,
    })
    token = auth["token"]
    print(f"Autenticado como {auth['email']} en {auth['organizationSlug']}")

    props = http("GET", f"{base}/properties", token=token)
    print(f"{len(props)} propiedades en la cartera")

    sucias = [(p, nueva) for p in props if (nueva := ciudad_limpia(p["city"], p["address"]))]
    if not sucias:
        print("Nada que sanear: ninguna ciudad tiene la dirección pegada.")
        return 0

    print(f"\n{len(sucias)} con la dirección pegada a la ciudad:\n")
    for p, nueva in sucias:
        print(f"  {p.get('code') or p['id'][:8]}  {p['city']!r}  →  {nueva!r}")

    if not args.aplicar:
        print("\nSimulacro. Volvé a correrlo con --aplicar para escribir los cambios.")
        return 0

    print()
    for p, nueva in sucias:
        # El PUT reemplaza la propiedad entera: hay que reenviar la ficha pública
        # completa o se borra (Details va como null y el handler la limpia).
        http("PUT", f"{base}/properties/{p['id']}", token=token, body={
            "address": p["address"],
            "city": nueva,
            "province": p["province"],
            "propertyType": p["propertyType"],
            "areaM2": p["areaM2"],
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
                "coveredAreaM2": p["coveredAreaM2"],
                "latitude": p["latitude"],
                "longitude": p["longitude"],
                "suitableForCredit": p["suitableForCredit"],
                "features": p["features"],
            },
        })
        print(f"  ✓ {p.get('code') or p['id'][:8]}  →  {nueva}")

    print(f"\nListo: {len(sucias)} propiedades corregidas.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
