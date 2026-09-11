#!/usr/bin/env python
"""
Carga el JSON de `scrape.py` en nuestra API: propiedades con ficha, fotos y publicaciones.

    python importar.py ./salida --api http://localhost:5000/api/v1 \
        --slug palavecino --email admin@palavecino.com --password 'Secreta123!'

Si la organización no existe la registra (nombre = --nombre). Pasa por la API y no por la base a
propósito: ejercita exactamente lo que va a usar la inmobiliaria y respeta el multi-tenant.
Es idempotente por código de propiedad: si ya existe una con el mismo `code`, la salta.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import requests

# La consola de Windows arranca en cp1252 y se ahoga con acentos y flechas.
sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # type: ignore[attr-defined]


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("carpeta", help="carpeta con propiedades.json y fotos/")
    ap.add_argument("--api", default="http://localhost:5000/api/v1")
    ap.add_argument("--slug", required=True)
    ap.add_argument("--email", required=True)
    ap.add_argument("--password", required=True)
    ap.add_argument("--nombre", default=None, help="nombre de la inmobiliaria si hay que registrarla")
    ap.add_argument("--invite", default=None, help="código de invitación (Registration:Mode=InviteCode en el VPS)")
    ap.add_argument("--publicar", action="store_true", help="crear las publicaciones como Published (default: Draft)")
    ap.add_argument("--sin-fotos", action="store_true")
    args = ap.parse_args()

    carpeta = Path(args.carpeta)
    props = json.loads((carpeta / "propiedades.json").read_text(encoding="utf-8"))
    api = args.api.rstrip("/")
    s = requests.Session()

    # --- sesión ---
    login = s.post(f"{api}/auth/login", json={"email": args.email, "password": args.password, "organizationSlug": args.slug})
    if login.status_code != 200:
        if not args.nombre:
            sys.exit(f"Login falló ({login.status_code}); pasá --nombre para registrar la organización.")
        payload = {
            "organizationName": args.nombre, "slug": args.slug, "adminEmail": args.email,
            "adminPassword": args.password, "adminFirstName": "Admin", "adminLastName": args.nombre,
        }
        if args.invite:
            payload["inviteCode"] = args.invite
        reg = s.post(f"{api}/auth/register-org", json=payload)
        reg.raise_for_status()
        login = s.post(f"{api}/auth/login", json={"email": args.email, "password": args.password, "organizationSlug": args.slug})
        login.raise_for_status()
    s.headers["Authorization"] = "Bearer " + login.json()["token"]

    existentes = {p["code"]: p for p in s.get(f"{api}/properties").json() if p.get("code")}
    creadas = saltadas = fotos = pubs = 0

    def subir_fotos(prop_id: str, rutas: list[str]) -> int:
        ok = 0
        for rel in rutas:
            archivo = carpeta / rel
            if not archivo.exists():
                continue
            with archivo.open("rb") as fh:
                up = s.post(f"{api}/properties/{prop_id}/photos", files={"file": (archivo.name, fh, "image/jpeg")})
            if up.status_code == 201:
                ok += 1
            else:
                print(f"  foto {archivo.name}: {up.status_code} {up.text[:120]}", file=sys.stderr)
        return ok

    for p in props:
        if p["code"] in existentes:
            saltadas += 1
            # Segunda pasada: la propiedad ya estaba pero sin fotos (import previo con --sin-fotos
            # o scrape incompleto). Completarlas sin duplicar nada.
            if not args.sin_fotos and p["photos"]:
                pid = existentes[p["code"]]["id"]
                if not s.get(f"{api}/properties/{pid}/photos").json():
                    n = subir_fotos(pid, p["photos"])
                    fotos += n
                    if n:
                        print(f"  {p['code']} +{n} fotos")
            continue

        r = s.post(f"{api}/properties", json={
            "address": p["address"] or p["title"][:300],
            "city": p["city"] or "Buenos Aires",
            "province": "Buenos Aires" if "provincia" in (p["city"] or "").lower() else ("CABA" if "capital" in (p["city"] or "").lower() else "Buenos Aires"),
            "propertyType": p["propertyType"],
            "areaM2": p["areaM2"],
            "notes": f"Importada de {p['sourceUrl']}",
            "details": {
                "neighborhood": p["neighborhood"],
                "code": p["code"],
                "description": p["description"][:5000] if p["description"] else None,
                "rooms": p["rooms"], "bedrooms": p["bedrooms"], "bathrooms": p["bathrooms"], "garages": p["garages"],
                "ageYears": p["ageYears"], "coveredAreaM2": p["coveredAreaM2"],
                "suitableForCredit": p["suitableForCredit"],
                "features": [f[:40] for f in p["features"] if "|" not in f][:40],
            },
        })
        if r.status_code != 201:
            print(f"  {p['code']}: propiedad {r.status_code} {r.text[:200]}", file=sys.stderr)
            continue
        prop = r.json()
        creadas += 1

        if not args.sin_fotos:
            fotos += subir_fotos(prop["id"], p["photos"])

        if p["price"]:
            l = s.post(f"{api}/listings", json={
                "propertyId": prop["id"],
                "operationType": p["operationType"],
                "price": p["price"],
                "currency": p["currency"],
                "expenses": p.get("expenses"),
                "title": p["title"][:200],
                "status": "Published" if args.publicar else "Draft",
            })
            if l.status_code == 201:
                pubs += 1
            else:
                print(f"  {p['code']}: publicación {l.status_code} {l.text[:200]}", file=sys.stderr)

        print(f"  {p['code']} ✓")

    print(f"\nPropiedades creadas: {creadas} · saltadas (ya existían): {saltadas} · fotos: {fotos} · publicaciones: {pubs}")


if __name__ == "__main__":
    main()
