# Identity & Access Service

Identity & Access implements EP08 (TS-01 to TS-04, US18) and ADR-007. It registers users,
authenticates them, issues bearer tokens and keeps the mining units each user may act on.

## Model

| Element | Rules |
|---|---|
| `User` (aggregate) | Unique lower-case username, full name, role (`Operator`, `Supervisor`, `SafetyManager`, `Administrator`), PBKDF2-SHA256 password hash, active flag and mining unit scopes. A deactivated user cannot sign in, refresh or receive new scopes. |
| `Session` (aggregate) | One per sign-in. Stores only the SHA-256 hash of the refresh token, its expiry and its revocation. Each refresh revokes the old session and opens a new one. |

Presenting a refresh token that was already rotated or signed out is treated as a stolen
token: every open session of that user is revoked.

## Tokens (ADR-007)

- Access token: JWT signed with RS256. Claims: `sub`, `unique_name`, `name`, `role`,
  `mining_unit` (one per scope), `jti`. It lasts `AccessTokenMinutes` (15 by default).
  Revocation relies on this short lifetime.
- Refresh token: 256 random bits. It lasts `RefreshTokenDays` (7 by default).
- `GET /.well-known/jwks.json` publishes the public key. The other microservices validate tokens
  locally with it and never call this service per request.
- Configure `IdentityAccess:Jwt:PrivateKeyPem` in every non-local environment. Without it, an
  ephemeral key is generated and every token becomes invalid on restart.

## API routes

| Method | Route | Access | Purpose |
|---|---|---|---|
| `POST` | `/api/v1/authentication/sign-up` | Public | Self-registration. Only an administrator may register an `Administrator` (403). |
| `POST` | `/api/v1/authentication/sign-in`, `/api/v1/auth/login` | Public | TS-01. Returns 200 with `accessToken`, `refreshToken`, `expiresIn` and the user; 401 without tokens. |
| `POST` | `/api/v1/authentication/refresh`, `/api/v1/auth/refresh` | Public | TS-02. Rotates the pair; 401 if the token is expired, revoked or tampered with. |
| `POST` | `/api/v1/authentication/sign-out` | Public | Revokes the session of a refresh token (204). |
| `GET` | `/api/v1/users/me` | Any token | TS-03. Profile, role and scopes; 401 without a valid token, 403 if the user is deactivated. |
| `POST` | `/api/v1/users` | Administrator | TS-04. Creates a user with any role; 409 on a duplicate username. |
| `GET` | `/api/v1/users?role=` | Administrator | Lists users, optionally by role. |
| `GET` | `/api/v1/users/{userId}` | Administrator | One user. |
| `PUT` | `/api/v1/users/{userId}/role` | Administrator | Changes the responsibility. |
| `POST` | `/api/v1/users/{userId}/deactivation` | Administrator | Deactivates the user. |
| `POST` | `/api/v1/users/{userId}/mining-units` | Administrator | US18. Grants a mining unit of Operations. |
| `DELETE` | `/api/v1/users/{userId}/mining-units/{miningUnitId}` | Administrator | US18. Revokes the scope and keeps the history. |

Rejected requests (401/403) are logged with method, path, UTC time, user and reason under the
`IdentityAccessService.AccessAudit` category (QAS04).

## Bootstrap administrator

Only administrators can create privileged users, so the first one comes from configuration
(`IdentityAccess:BootstrapAdministrator:Username` / `Password`). The Development profile seeds
`admin` / `Admin-Local-2026`. In other environments, supply real values through environment
variables, for example `IdentityAccess__BootstrapAdministrator__Password`.

## Verification

```powershell
dotnet test src/backend/tests/IdentityAccessService.Tests/IdentityAccessService.Tests.csproj
```

The adapter is in memory, like the other services in this slice. PostgreSQL persistence and
publishing access events to Privacy & Audit are still pending platform work.
