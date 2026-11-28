# syntax=docker/dockerfile:1
# Fleet IoT simulator: one virtual cabin gateway per truck of the demo mine (README.md).
#   docker compose up --build -d iot-simulator
# The demo seed comes from the dashboard scripts through the "frontend-scripts" build context.
# Alpine's nodejs package without npm: the simulator has no dependencies, so the image carries
# no package manager and its bundled modules (fewer CVEs for the Trivy gate of section 3.1).
FROM alpine:3.24

LABEL org.opencontainers.image.source="https://github.com/Resolum-MineSense-Safety/minesense-safety" \
      org.opencontainers.image.title="iot-simulator" \
      org.opencontainers.image.vendor="Resolum"

ARG PATCH_STAMP=""
RUN echo "alpine security patches ${PATCH_STAMP}" \
 && apk upgrade --no-cache \
 && apk add --no-cache nodejs \
 && addgroup -S -g 10001 gateway \
 && adduser -S -D -H -u 10001 -G gateway gateway

WORKDIR /app
COPY package.json *.mjs ./
COPY --from=frontend-scripts seed-demo.mjs ./seed-demo.mjs

ENV NODE_ENV=production PORT=8080
USER 10001
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=3s --retries=5 CMD wget -qO- http://127.0.0.1:8080/health/live || exit 1
CMD ["node", "simulator.mjs"]
