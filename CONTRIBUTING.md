# Contributing to MineSense Safety

Team workflow agreed in section 2.2 of the project report (SCM, Collaboration Policies,
Definition of Ready / Done). Read [docs/ddd/context-map.md](docs/ddd/context-map.md) before
touching the backend.

## Branches (GitFlow)

| Branch | Created from | Merges into | Example |
|---|---|---|---|
| `main` | — | — | production releases only |
| `develop` | `main` | — | integration branch |
| `feature/<us-or-ts>-<short-name>` | `develop` | `develop` | `feature/us05-acknowledge-alert` |
| `release/<x.y.z>` | `develop` | `main` and `develop` | `release/1.1.0` |
| `hotfix/<short-name>` | `main` | `main` and `develop` | `hotfix/alert-escalation-timeout` |

`main` and `develop` are protected: no direct pushes, every change goes through a Pull Request.

## Commits (Conventional Commits with scope)

```
type(scope): short imperative description
```

- Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `style`, `perf`, `build`, `ci`.
- Scope = the bounded context or area touched, in kebab-case: `alert-service`,
  `fatigue-detection-service`, `frontend`, `edge`, `workflows`, `shared-kernel`.
- One line, max 72 characters, no trailing period. English, like the rest of the history.

Examples: `feat(incident-management-service): close incident with resolution`,
`test(alert-service): cover escalation of acknowledged alerts`.

## Pull Requests

1. Branch from `develop`, keep the PR focused on one user story (USxx) or technical story (TSxx).
2. Fill in the PR template (story id, changes, how it was tested, checklist).
3. CI must be green: backend build + xUnit, frontend build, SonarQube.
4. At least **1 approval** from a teammate other than the author (2 for `release/*` → `main`).
5. Resolve every review conversation, then **Squash and merge**.

## Backend rules (DDD)

- One microservice = one bounded context. Never reference another service's project.
- Put business rules in the aggregate, not in controllers or application services; break a
  rule by throwing `DomainException` (the API returns HTTP 422).
- New use case → `Command`/`Query` record + handler in the command/query service +
  endpoint + assembler. Follow the existing `AlertService` as the reference implementation.
- Every rule gets an xUnit test in `src/backend/tests/<Service>.Tests` using the AAA pattern
  (`// Arrange`, `// Act`, `// Assert`).

## Running locally

```bash
docker compose up -d
dotnet test src/backend/MineSenseSafety.sln
dotnet run --project src/backend/AlertService
```

Swagger is available at `/swagger` on each service in the Development environment.
