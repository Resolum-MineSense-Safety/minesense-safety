#!/bin/bash
# Boot script of the demo instance (Amazon Linux 2023). deploy.sh fills the __PLACEHOLDERS__.
# Installs Docker with Compose and Buildx, clones the repository at the given ref and starts the
# complete environment: 13 microservices, Kafka, PostgreSQL, MongoDB, Mosquitto, the dashboard,
# the gateway and the fleet IoT simulator (which loads the demo mine on first start).
set -euxo pipefail
exec > >(tee /var/log/minesense-demo.log | logger -t minesense-demo -s 2>/dev/console) 2>&1

REPO_URL="__REPO_URL__"
REF="__REF__"

# 4 GB of swap: the .NET image builds run in parallel.
fallocate -l 4G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile

dnf install -y docker git
systemctl enable --now docker
mkdir -p /usr/local/lib/docker/cli-plugins
curl -fsSL "https://github.com/docker/compose/releases/download/v2.40.3/docker-compose-linux-x86_64" -o /usr/local/lib/docker/cli-plugins/docker-compose
curl -fsSL "https://github.com/docker/buildx/releases/download/v0.29.1/buildx-v0.29.1.linux-amd64" -o /usr/local/lib/docker/cli-plugins/docker-buildx
chmod +x /usr/local/lib/docker/cli-plugins/*
docker compose version
docker buildx version

git clone --depth 1 --branch "$REF" "$REPO_URL" /opt/minesense
cd /opt/minesense
cat > .env <<ENV
ADMIN_PASSWORD=__ADMIN_PASSWORD__
DEMO_PASSWORD=__DEMO_PASSWORD__
POSTGRES_PASSWORD=__POSTGRES_PASSWORD__
ENV
chmod 600 .env

COMPOSE="docker compose -f docker-compose.yml -f infrastructure/demo-ec2/docker-compose.demo.yml"
$COMPOSE build
$COMPOSE up -d

# Start the stack again after a reboot.
cat > /etc/systemd/system/minesense.service <<UNIT
[Unit]
Description=MineSense Safety demo
After=docker.service
Requires=docker.service
[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/minesense
ExecStart=$COMPOSE up -d
ExecStop=$COMPOSE stop
[Install]
WantedBy=multi-user.target
UNIT
systemctl daemon-reload
systemctl enable minesense.service

until curl -fsS http://localhost/healthz; do sleep 10; done
echo "MINESENSE-DEMO-READY"
