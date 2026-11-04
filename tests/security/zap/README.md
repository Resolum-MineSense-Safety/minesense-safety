# Pruebas de seguridad dinámicas (DAST) con OWASP ZAP

Sección 4.4.4 del informe. El workflow
[`dast-zap.yml`](../../../.github/workflows/dast-zap.yml) levanta el entorno con docker compose y corre:

1. **ZAP baseline** contra el dashboard (`http://localhost:8080`). Es un escaneo pasivo con spider: revisa
   cabeceras, cookies y divulgación de información.
2. **ZAP API scan** contra el contrato OpenAPI de Alerts y de Fatigue Detection. Es un escaneo activo:
   prueba inyección, XSS, *path traversal* y errores que revelan información en cada endpoint.

`rules.tsv` decide qué hace cada regla: `FAIL` rompe el pipeline, `WARN` solo avisa e `IGNORE` se
descarta, siempre con su justificación. Los reportes HTML, JSON y Markdown se publican como artefacto
`zap-reports`.

## Cómo correrlo en local

```bash
docker compose up -d --build
docker run --rm --network host -v "$PWD/tests/security/zap:/zap/wrk:rw" ghcr.io/zaproxy/zaproxy:stable \
  zap-baseline.py -t http://localhost:8080 -c rules.tsv -r baseline.html -J baseline.json
```

## Relación con OWASP Top 10 (2021)

| Riesgo | Control en MineSense | Evidencia |
|---|---|---|
| A01 Broken Access Control | JWT RS256 obligatorio en cada API (ADR-007), roles por endpoint, RBAC en Kubernetes | `AuthenticationTests`, NFR02 y la prueba E2E de 401 |
| A02 Cryptographic Failures | TLS 1.2/1.3 en el ALB con ACM, cifrado en reposo en DocumentDB y RDS (`storage_encrypted`), contraseñas con PBKDF2-SHA256 | Terraform `modules/data`, `ssl-policy` del Ingress, `Pbkdf2HashingService` |
| A03 Injection | Consultas parametrizadas (Npgsql), driver tipado de MongoDB, validación `[JsonRequired]` | Reglas 40018 y 40012 de ZAP en `FAIL`, SonarCloud |
| A04 Insecure Design | Amenazas tratadas en los ADR, procesamiento en edge sin depender de la nube | ADR-002/003 |
| A05 Security Misconfiguration | Contenedores sin root y con FS de solo lectura, cabeceras de seguridad, Pod Security `restricted` | ZAP baseline (10020, 10021, 10038), Trivy de IaC |
| A06 Vulnerable Components | Dependabot, Trivy de imágenes y dependencias con `exit-code 1` | `security-scan.yml`, `cd-deploy.yml` |
| A07 Identification and Authentication Failures | Access tokens con vencimiento, refresh tokens guardados con hash y sesiones revocables, firma RS256 con JWKS. **Pendiente:** bloquear la cuenta tras varios intentos fallidos (roadmap) | Identity & Access (`AuthenticationCommandService`) |
| A08 Software and Data Integrity Failures | Imágenes inmutables por SHA, OIDC sin llaves en GitHub Actions, *build once deploy many* | `cd-deploy.yml` |
| A09 Logging and Monitoring Failures | Log `Access rejected ...` de cada rechazo, Privacy & Audit, alertas de Prometheus | `MineSenseUnauthorizedSpike`, runbook RB-04 |
| A10 SSRF | Los servicios solo llaman URL de configuración; las NetworkPolicy limitan el *egress* | `infrastructure/k8s/network-policies.yaml` |
