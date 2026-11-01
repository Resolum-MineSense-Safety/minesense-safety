# syntax=docker/dockerfile:1
# MineSense Safety Edge agent (cabin gateway). The build context is src/edge:
#   docker build -t minesense/edge-agent src/edge

FROM python:3.13-slim AS build
WORKDIR /build
ENV PIP_NO_CACHE_DIR=1 PIP_DISABLE_PIP_VERSION_CHECK=1
# Runtime dependencies only (pytest stays out of the image), installed in a venv that is copied as is.
COPY requirements.txt .
RUN python -m venv /opt/venv \
    && /opt/venv/bin/pip install --upgrade pip \
    && /opt/venv/bin/pip install -r requirements.txt     && /opt/venv/bin/pip uninstall -y pip

FROM python:3.13-slim AS runtime

LABEL org.opencontainers.image.source="https://github.com/Resolum-MineSense-Safety/minesense-safety" \
      org.opencontainers.image.title="edge-agent" \
      org.opencontainers.image.vendor="Resolum"

# Apply the Debian security fixes published after the base image was built and remove pip:
# nothing is installed at runtime, and pip's vendored libraries only add vulnerabilities.
# CI passes a new PATCH_STAMP on every run so a cached layer never hides newer fixes.
ARG PATCH_STAMP=""
RUN echo "debian security patches ${PATCH_STAMP}" \
    && apt-get update && apt-get upgrade -y --no-install-recommends \
    && rm -rf /var/lib/apt/lists/* \
    && python -m pip uninstall -y pip     && rm -rf /usr/local/lib/python3.13/ensurepip \
    && groupadd --system --gid 10001 edge \
    && useradd --system --uid 10001 --gid edge --home-dir /app --shell /usr/sbin/nologin edge \
    && mkdir -p /app/data && chown edge:edge /app/data

ENV PATH="/opt/venv/bin:$PATH" \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    EDGE_BUFFER_PATH=/app/data/edge-buffer.db

WORKDIR /app
COPY --from=build /opt/venv /opt/venv
COPY main.py ./
COPY minesense_edge/ minesense_edge/

# The offline buffer (SQLite) is the only writable path; mount a volume here so pending
# events survive a restart of the container (US13, US14).
VOLUME ["/app/data"]
USER 10001:10001

ENTRYPOINT ["python", "main.py"]
