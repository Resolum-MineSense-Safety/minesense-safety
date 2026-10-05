# MineSense Safety - Monorepo

Integrated microservices and IoT system for the preventive detection and mitigation of fatigue in heavy machinery operators.

## Project Structure

- `.github/workflows/`: CI/CD pipelines as code using GitHub Actions.
- `src/backend/`: Microservices developed in C# with .NET 8.
- `src/edge/`: Local cabin processing and inference agent developed in Python.
- `src/frontend/`: Web-based monitoring dashboard built with React, Vite, and Tailwind CSS.
- `infrastructure/`: Provisioning code using Terraform and Kubernetes/Helm manifests.
- `docs/`: C4 architecture documentation and Architecture Decision Records (ADRs).
- `docker-compose.yml`: Local orchestration environment.
