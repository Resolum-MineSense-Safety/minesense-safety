# MineSense Safety - Monorepo

Integrated microservices and IoT system for the preventive detection and mitigation of fatigue in heavy machinery operators.

## Project Structure

- `.github/workflows/`: CI/CD pipelines as code using GitHub Actions.
- `.github/pull_request_template.md`: PR template required by the collaboration policies.
- `src/backend/`: Microservices developed in C# with .NET 8, one per DDD bounded context.
  - `Shared/`: shared kernel (aggregate base type, repository contract, Web API conventions).
  - `<Service>/`: `Domain`, `Application`, `Infrastructure` and `Interfaces` layers.
  - `tests/<Service>.Tests/`: xUnit unit tests (AAA pattern) for each bounded context.
- `src/edge/`: Local cabin processing and inference agent developed in Python.
- `src/frontend/`: Web-based monitoring dashboard built with React, Vite, and Tailwind CSS.
- `infrastructure/`: Provisioning code using Terraform and Kubernetes/Helm manifests.
- `docs/`: DDD context map, C4 architecture documentation and Architecture Decision Records (ADRs). Start at [docs/README.md](docs/README.md).
- `docker-compose.yml`: Local orchestration environment.

## Bounded Contexts

| Service | Bounded context | Epic | Base route |
|---|---|---|---|
| `IdentityAccessService` | Identity & Access | EP08 | `/api/v1/authentication`, `/api/v1/users` |
| `FatigueDetectionService` | Fatigue Detection | EP02 | `/api/v1/fatigue-assessments` |
| `AlertService` | Alerts | EP03 | `/api/v1/alerts` |
| `FleetMonitoringService` | Fleet Monitoring | EP04 | `/api/v1/fleet-operators` |
| `IncidentManagementService` | Incident Management | EP05 | `/api/v1/incidents` |

See [docs/ddd/context-map.md](docs/ddd/context-map.md) for relationships, layer layout and the ubiquitous language.

## Getting Started

### Run the whole local environment (Docker)

Requires Docker Desktop (or Docker Engine with Compose v2) running.

```bash
docker compose up --build
```

| Component | URL / port |
|---|---|
| IdentityAccessService | http://localhost:5150/swagger |
| FatigueDetectionService | http://localhost:5158/swagger |
| AlertService | http://localhost:5233/swagger |
| FleetMonitoringService | http://localhost:5255/swagger |
| IncidentManagementService | http://localhost:5070/swagger |
| PostgreSQL | `localhost:5432` |
| MongoDB | `localhost:27017` |
| Apache Kafka (KRaft, single node) | `localhost:9094` from the host, `kafka:9092` inside the network |
| MQTT broker (Mosquitto) | `localhost:1883`, WebSockets `localhost:9001` |
| Edge simulator | no port; follow it with `docker compose logs -f edge-simulator` |

The Edge simulator generates synthetic PERCLOS, blink rate and HRV readings, classifies the
risk locally (same thresholds as `FatigueRiskPolicy`), raises the in-cabin alarm offline,
posts every assessment to FatigueDetectionService and, on `Critical`, issues an alert in
AlertService. While the cloud is unreachable the events stay in an offline buffer
(`edge-buffer` volume) and are flushed in order when it comes back. Tune it with
`EDGE_PROFILE` (`normal`, `drowsy`, `microsleep`, `cycle`), `EDGE_OPERATOR_ID`,
`EDGE_INTERVAL_SECONDS` and `EDGE_CYCLE_LENGTH`, for example:

```bash
EDGE_PROFILE=microsleep docker compose up --build edge-simulator
curl "http://localhost:5233/api/v1/alerts?operatorId=3f2504e0-4f89-41d3-9a0c-0305e82c3301"
```

Persistence is in-memory for now, so data is lost when a service container restarts.

### Run and test without Docker

```bash
dotnet test src/backend/MineSenseSafety.sln
dotnet run --project src/backend/AlertService

pip install -r src/edge/requirements.txt
python -m pytest src/edge -q
cd src/edge && python main.py
```

Each service exposes Swagger at `/swagger` in the Development environment and ships a `.http` file with sample requests.

## Contributing

GitFlow, Conventional Commits with scope and the Pull Request rules are described in [CONTRIBUTING.md](CONTRIBUTING.md).
