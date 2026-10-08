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
- `docs/`: DDD context map, C4 architecture documentation and Architecture Decision Records (ADRs).
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

```bash
docker compose up -d
dotnet test src/backend/MineSenseSafety.sln
dotnet run --project src/backend/AlertService
```

Each service exposes Swagger at `/swagger` in the Development environment and ships a `.http` file with sample requests.

## Contributing

GitFlow, Conventional Commits with scope and the Pull Request rules are described in [CONTRIBUTING.md](CONTRIBUTING.md).
