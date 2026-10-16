# MineSense Safety - Monorepo

Integrated microservices and IoT system for the preventive detection and mitigation of fatigue in heavy machinery operators.

## Project Structure

- `.github/workflows/`: CI/CD pipelines as code using GitHub Actions.
- `src/backend/`: Microservices developed in C# with .NET 8.
- `src/backend/PrivacyAuditService/`: Privacy & Audit bounded context (EP09/EP10, scaffold).
- `src/backend/ReportingService/`: Reporting bounded context (EP10, scaffold).
- `src/backend/FeedbackReliabilityService/`: Feedback & Reliability (EP12, US26–US28); [setup, contracts and testing](src/backend/FeedbackReliabilityService/README.md).
- `src/edge/`: Local cabin processing and inference agent developed in Python.
- `src/frontend/`: Web-based monitoring dashboard built with React, Vite, and Tailwind CSS.
- `infrastructure/`: Provisioning code using Terraform and Kubernetes/Helm manifests.
- `docs/`: C4 architecture documentation and Architecture Decision Records (ADRs).
- `docker-compose.yml`: Local orchestration environment.
