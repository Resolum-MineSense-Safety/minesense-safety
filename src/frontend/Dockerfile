# syntax=docker/dockerfile:1
# Supervision Dashboard image (section 3.1): the Vite build is served as static files by nginx.
#   docker build -t minesense/supervision-dashboard src/frontend

FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies first so the npm layer stays cached while only the code changes.
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npm run build

# Alpine's own nginx package instead of nginx-unprivileged: Alpine 3.24 already ships nginx 1.30.7,
# which fixes six CRITICAL/HIGH CVEs (mail, HTTP/2, resolver, TLS) still present in the newest
# nginx-unprivileged image (1.30.6). Same contract as before: UID/GID 101, port 8080, writes only /tmp.
FROM alpine:3.24 AS runtime

LABEL org.opencontainers.image.source="https://github.com/Resolum-MineSense-Safety/minesense-safety" \
      org.opencontainers.image.title="supervision-dashboard" \
      org.opencontainers.image.vendor="Resolum"

# CI passes a new PATCH_STAMP on every run so a cached layer never hides newer Alpine fixes.
# The nginx user is created first with UID 101, the one Helm sets (runAsUser/fsGroup).
ARG PATCH_STAMP=""
RUN echo "alpine security patches ${PATCH_STAMP}" \
 && addgroup -S -g 101 nginx \
 && adduser -S -D -H -u 101 -G nginx -h /var/lib/nginx -s /sbin/nologin nginx \
 && apk upgrade --no-cache \
 && apk add --no-cache nginx \
 && rm -rf /etc/nginx/http.d

COPY nginx-main.conf /etc/nginx/nginx.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

USER 101
EXPOSE 8080
STOPSIGNAL SIGQUIT
CMD ["nginx", "-g", "daemon off;"]
