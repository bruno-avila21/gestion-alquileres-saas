# Build del SPA y servicio estático con Caddy.
FROM node:20-alpine AS build
WORKDIR /src

RUN corepack enable

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .

# Vite inlinea las variables AL COMPILAR: la URL de la API se fija acá y no se puede cambiar
# después con una variable de entorno del hosting. Relativa a propósito — Caddy sirve el SPA y
# hace de proxy a la API en el mismo origen, así que no hace falta saber el dominio al construir.
ARG VITE_API_URL=/api/v1
ENV VITE_API_URL=$VITE_API_URL
RUN pnpm build

FROM caddy:2-alpine AS runtime
COPY --from=build /src/dist /srv
COPY Caddyfile /etc/caddy/Caddyfile
EXPOSE 8080
