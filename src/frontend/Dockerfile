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

# nginx-unprivileged runs as a non-root user and listens on 8080.
FROM nginxinc/nginx-unprivileged:1.30.6-alpine3.24 AS runtime

LABEL org.opencontainers.image.source="https://github.com/Resolum-MineSense-Safety/minesense-safety" \
      org.opencontainers.image.title="supervision-dashboard" \
      org.opencontainers.image.vendor="Resolum"

# Apply the security fixes published after the base image was built, then drop back to non-root.
# CI passes a new PATCH_STAMP on every run so a cached layer never hides newer Alpine fixes.
ARG PATCH_STAMP=""
USER root
RUN echo "alpine security patches ${PATCH_STAMP}" && apk upgrade --no-cache
USER 101

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
