#!/usr/bin/env python
"""
Lee el sitio público de una inmobiliaria hecho con Tokko Broker y lo vuelca a JSON + fotos.

    python scrape.py https://www.bpalavecino.com --out ./salida --max-fotos 8

Sirve para armar la demo con la cartera real de un prospecto sin pedirle API key ni export:
todo lo que se lee acá es lo que la inmobiliaria ya publica. El JSON lo consume `importar.py`,
que lo carga a nuestra API (propiedades + fichas + fotos + publicaciones).

Sólo stdlib + requests. El HTML de las plantillas Tokko es regular (server-side, jQuery), así que
alcanza con expresiones regulares; si Tokko cambia la plantilla, ajustar los patrones de abajo.
"""
from __future__ import annotations

import argparse
import html
import json
import re
import sys
import time
from pathlib import Path

import requests

# La consola de Windows arranca en cp1252 y se ahoga con acentos y flechas.
sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # type: ignore[attr-defined]

HEADERS = {"User-Agent": "Mozilla/5.0 (compatible; importador-demo/1.0)"}
LISTADOS = {"Sale": "/Venta", "Rent": "/Alquiler", "TemporaryRent": "/Alquiler-Temporario"}

TIPOS = {
    "departamento": "Apartment", "casa": "House", "ph": "PH", "terreno": "Land", "lote": "Land",
    "local": "Commercial", "oficina": "Office", "galpón": "Commercial", "galpon": "Commercial",
    "cochera": "Other", "quinta": "House", "campo": "Land",
}


def get(session: requests.Session, url: str) -> str:
    for intento in range(3):
        try:
            r = session.get(url, headers=HEADERS, timeout=40)
            r.raise_for_status()
            return r.content.decode("utf-8", errors="replace")
        except requests.HTTPError as e:
            if e.response is not None and e.response.status_code == 404:
                raise  # no existe (ej. sin alquiler temporario): reintentar no ayuda
            if intento == 2:
                raise
            print(f"  reintento {url}: {e}", file=sys.stderr)
            time.sleep(2)
        except requests.RequestException as e:  # red caída: reintento corto
            if intento == 2:
                raise
            print(f"  reintento {url}: {e}", file=sys.stderr)
            time.sleep(2)
    raise RuntimeError("unreachable")


def limpiar(s: str) -> str:
    return re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", s))).strip()


def numero(s: str | None) -> float | None:
    """'33,66 m²' -> 33.66 · '$ 800.000' -> 800000 · 'A estrenar' -> None"""
    if not s:
        return None
    m = re.search(r"-?\d[\d\.]*(?:,\d+)?", s)
    if not m:
        return None
    return float(m.group(0).replace(".", "").replace(",", "."))


def precio(s: str) -> tuple[str, float] | None:
    s = limpiar(s)
    if not s or "consultar" in s.lower():
        return None
    moneda = "USD" if re.search(r"U\$S|USD|U\$D", s, re.I) else "ARS"
    n = numero(s)
    return (moneda, n) if n else None


# ---------- listado ----------

CARD = re.compile(r'<div class="[^"]*" prop-id="(\d+)">(.*?)</a>\s*</div>', re.S)


def cards_de(pagina: str) -> list[dict]:
    out = []
    for prop_id, cuerpo in CARD.findall(pagina):
        href = re.search(r'href="(/p/[^"]+)"', cuerpo)
        alt = re.search(r'alt="([^"]*)"', cuerpo)
        direccion = re.search(r'class="card-address">(.*?)</p>', cuerpo, re.S)
        tipo = re.search(r'class="prop-card-red-text">([^<]*)<', cuerpo)
        codigo = re.search(r'type_and_code">.*?<p class="">([^<]*)<', cuerpo, re.S)
        titulo = re.search(r"<h4>(.*?)</h4>", cuerpo, re.S)
        prec = re.search(r'class="fp_price">(.*?)</span>', cuerpo, re.S)
        out.append({
            "tokkoId": prop_id,
            "href": href.group(1) if href else None,
            "alt": html.unescape(alt.group(1)) if alt else "",
            "address": limpiar(direccion.group(1)) if direccion else "",
            "tipoTexto": limpiar(tipo.group(1)) if tipo else "",
            "code": limpiar(codigo.group(1)) if codigo else None,
            "title": limpiar(titulo.group(1)) if titulo else "",
            "precioTexto": limpiar(prec.group(1)) if prec else "",
        })
    return out


def listado(session: requests.Session, base: str, operacion: str, ruta: str) -> list[dict]:
    todos: list[dict] = []
    vistos: set[str] = set()
    # El listado pagina por AJAX con `?p=N` (20 por página) y devuelve un fragmento HTML que
    # termina en "--NoMoreProperties--" cuando se acabó. `?page=` se ignora.
    for page in range(1, 60):
        html_pagina = get(session, f"{base}{ruta}?p={page}")
        cards = cards_de(html_pagina)
        if "--NoMoreProperties--" in html_pagina and not cards:
            break
        nuevos = [c for c in cards if c["tokkoId"] not in vistos]
        if not nuevos:
            break
        for c in nuevos:
            c["operationType"] = operacion
            vistos.add(c["tokkoId"])
        todos.extend(nuevos)
        print(f"  {ruta} página {page}: {len(nuevos)} propiedades")
    return todos


# ---------- ficha ----------

ATRIBUTO = re.compile(r"<li><p>([^<:]{2,40}):</p></li>\s*</ul>\s*<ul[^>]*>\s*<li><p>(?:<span>)?(.*?)(?:</span>)?</p></li>", re.S)
GRUPO = re.compile(r'<h4 class="mb10"[^>]*>([^<]+)</h4>.*?<ul class="order_list">(.*?)</ul>', re.S)
ITEM = re.compile(r'<span class="flaticon-tick"></span>([^<]+)</a>')


def ficha(session: requests.Session, base: str, card: dict) -> dict:
    d = get(session, base + card["href"])

    atributos = {limpiar(k).lower(): limpiar(v) for k, v in ATRIBUTO.findall(d)}
    grupos = {limpiar(g): [limpiar(i) for i in ITEM.findall(cuerpo)] for g, cuerpo in GRUPO.findall(d)}
    features = [f for items in grupos.values() for f in items]

    desc = re.search(r'description-section">(.*?)</div>', d, re.S)
    descripcion = limpiar(desc.group(1)) if desc else ""
    descripcion = re.sub(r"^Descripci[oó]n\s*", "", descripcion)
    descripcion = re.sub(r"\s*Mostrar m[aá]s\s*Mostrar menos\s*$", "", descripcion)

    fotos = []
    # `pictures/` es la foto original; las fichas más nuevas sólo traen `w_pics/` (tamaño web, con marca de agua).
    for u in re.findall(r'https://static\.tokkobroker\.com/(?:pictures|w_pics)/[^"\']+\.(?:jpg|jpeg|png|webp)', d, re.I):
        if u not in fotos:
            fotos.append(u)

    def attr(*claves: str) -> str | None:
        for k in claves:
            if k in atributos:
                return atributos[k]
        return None

    antig = attr("antigüedad", "antiguedad")
    antig_n = numero(antig)
    age = 0 if antig and "estrenar" in antig.lower() else (int(antig_n) if antig_n is not None else None)
    credito = attr("crédito", "credito")
    apto = None if not credito else ("no" not in credito.lower())

    return {
        "description": descripcion,
        "rooms": int(numero(attr("ambientes")) or 0) or None,
        "bedrooms": int(numero(attr("dormitorios")) or 0) or None,
        "bathrooms": int(numero(attr("baños", "banos")) or 0) or None,
        "garages": int(numero(attr("cocheras")) or 0) or None,
        "ageYears": age,
        "coveredAreaM2": numero(attr("superficie cubierta", "cubierta")),
        "areaM2": numero(attr("superficie total", "terreno", "superficie del terreno", "superficie terreno", "total construido")),
        "expenses": numero(attr("expensas")),
        "suitableForCredit": apto,
        "features": features,
        "grupos": grupos,
        "atributos": atributos,
        "fotos": fotos,
    }


def barrio_y_ciudad(card: dict) -> tuple[str | None, str]:
    """alt = 'Foto Departamento en Alquiler en Villa Pueyrredon, Capital Federal Avenida ...'"""
    alt = card["alt"]
    if card["address"] and alt.endswith(card["address"]):
        alt = alt[: -len(card["address"])].strip()
    m = re.search(r".* en ([^,]+?)(?:, (.+))?$", alt)  # el último " en ": "Casa en Venta en Bella Vista, San Miguel"
    if not m:
        return None, ""
    barrio = m.group(1).strip()
    ciudad = (m.group(2) or barrio).strip()
    return barrio, ciudad


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("base", help="URL raíz del sitio, ej. https://www.bpalavecino.com")
    ap.add_argument("--out", default="salida")
    ap.add_argument("--max-fotos", type=int, default=8, help="fotos a bajar por propiedad (0 = ninguna)")
    ap.add_argument("--limite", type=int, default=0, help="sólo las primeras N propiedades (pruebas)")
    args = ap.parse_args()

    base = args.base.rstrip("/")
    out = Path(args.out)
    (out / "fotos").mkdir(parents=True, exist_ok=True)
    s = requests.Session()

    print("Listados…")
    cards: list[dict] = []
    for op, ruta in LISTADOS.items():
        try:
            cards += listado(s, base, op, ruta)
        except requests.HTTPError as e:  # el sitio puede no tener temporarios
            print(f"  {ruta}: {e}, se omite")
    if args.limite:
        cards = cards[: args.limite]
    print(f"{len(cards)} publicaciones")

    propiedades = []
    for i, card in enumerate(cards, 1):
        print(f"[{i}/{len(cards)}] {card['code']} {card['title'][:60]}")
        f = ficha(s, base, card)
        barrio, ciudad = barrio_y_ciudad(card)
        p = precio(card["precioTexto"])
        tipo = TIPOS.get(card["tipoTexto"].lower().strip(), "Other")

        locales = []
        for n, url in enumerate(f["fotos"][: args.max_fotos]):
            destino = out / "fotos" / f"{card['tokkoId']}_{n}.jpg"
            if not destino.exists():
                try:
                    destino.write_bytes(s.get(url, headers=HEADERS, timeout=60).content)
                except requests.RequestException as e:
                    print(f"  foto {url}: {e}", file=sys.stderr)
                    continue
            locales.append(str(destino.relative_to(out)))

        propiedades.append({
            "tokkoId": card["tokkoId"],
            "code": card["code"],
            "title": card["title"],
            "operationType": card["operationType"],
            "currency": p[0] if p else None,
            "price": p[1] if p else None,
            "propertyType": tipo,
            "tipoTexto": card["tipoTexto"],
            "address": card["address"],
            "neighborhood": barrio,
            "city": ciudad,
            "sourceUrl": base + card["href"],
            "photos": locales,
            "photoUrls": f["fotos"],
            **{k: v for k, v in f.items() if k != "fotos"},
        })
        time.sleep(0.4)  # no castigar el servidor del prospecto

    (out / "propiedades.json").write_text(json.dumps(propiedades, ensure_ascii=False, indent=2), encoding="utf-8")
    con_precio = sum(1 for p in propiedades if p["price"])
    print(f"\nListo: {len(propiedades)} propiedades ({con_precio} con precio) → {out / 'propiedades.json'}")


if __name__ == "__main__":
    main()
