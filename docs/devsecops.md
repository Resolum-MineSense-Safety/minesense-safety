# DevSecOps y seguridad (sección 3.7)

![DevSecOps](https://raw.githubusercontent.com/Resolum-MineSense-Safety/minesense-safety/develop/docs/diagrams/devsecops.png)

La seguridad se integra en cada etapa del ciclo (*shift left*): cada control corre lo más
temprano posible y bloquea el avance si encuentra un problema.

## Controles automáticos

| Control | Herramienta | Dónde corre | Qué bloquea |
| --- | --- | --- | --- |
| Calidad y seguridad del código (SAST) | SonarCloud | `sonarqube-analysis.yml`, check obligatorio | Bugs, vulnerabilidades y *security hotspots* |
| SAST de seguridad | CodeQL (`security-extended`) | `security-scan.yml`, en cada PR, push y cada lunes | Inyecciones, deserialización insegura, *path traversal* y otros patrones en C# y JavaScript |
| Secretos | gitleaks v8.21.2 | `security-scan.yml` | Claves, tokens o contraseñas en los commits del PR. El historial completo se revisa en push y cada lunes |
| Dependencias nuevas | Dependency Review | `security-scan.yml`, en PR | Paquetes con vulnerabilidad alta o crítica |
| Dependencias existentes | Dependabot | `.github/dependabot.yml`, mensual y ante avisos | Propone PRs agrupados para NuGet, npm, imágenes y actions |
| Imágenes | Trivy v0.36.0 | `containers-ci.yml` y `cd-deploy.yml` | CVE críticas o altas con corrección disponible |
| Contenedor endurecido | Docker en CI | `containers-ci.yml` | Imágenes que corren como root o que no arrancan con solo lectura y sin capabilities |
| Infraestructura | Terraform validate, Helm lint, kubeconform | `terraform-validate.yml`, `helm-validate.yml` | Configuraciones inválidas antes de llegar a AWS |

### Resultados medidos

- **gitleaks sobre las 177 confirmaciones del historial:** no hay secretos reales. Solo dos
  falsos positivos, contraseñas ficticias de las pruebas de Identity & Access, excluidos por su
  huella exacta en `.gitleaksignore`. Un secreto nuevo en cualquier otro lugar sigue fallando.
- **Imagen de Operations Service:** con la base `aspnet:8.0` (Debian 12) Trivy reporta 243
  vulnerabilidades (1 crítica, 58 altas). Con `aspnet:8.0-jammy-chiseled-extra` son 6 (2 medias,
  4 bajas).

## Controles en el diseño y en la plataforma

- **Código:** las reglas de negocio validan la entrada (400 si faltan datos y 422 si se rompe
  una regla), las contraseñas se guardan con PBKDF2-SHA256 y los tokens JWT se firman con RS256,
  duran 15 minutos y se validan localmente con JWKS (ADR-007). Las pruebas xUnit cubren 401 y
  403 y la revocación de sesiones al reutilizar un refresh token.
- **Repositorio:** `develop` y `main` están protegidas (revisión obligatoria, checks, sin force
  push). Los secretos nunca se versionan: `.env`, `*.tfvars` y `*.tfstate` están en
  `.gitignore`. Las vulnerabilidades se reportan según
  [SECURITY.md](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/SECURITY.md).
- **Pipelines:** el `GITHUB_TOKEN` tiene `contents: read` por defecto y AWS se usa por OIDC
  con un rol por entorno, sin claves de larga duración.
- **Kubernetes:** Pod Security `restricted`, `securityContext` con sistema de archivos de solo
  lectura, sin capabilities y seccomp `RuntimeDefault`. Secretos desde AWS Secrets Manager e
  IRSA para que cada pod tenga su propio rol IAM.
- **Red y datos:** TLS 1.2/1.3 en el ALB, bases de datos y Kafka en subredes privadas, cifrado
  con KMS y MQTTS con un certificado X.509 por cabina.
- **Datos personales (Ley N.° 29733):** Privacy & Audit registra los accesos con una cadena de
  hashes, y las solicitudes rechazadas (401 y 403) se auditan (QAS04).

## Qué hacer cuando un control falla

1. Abrir el job fallido y revisar el detalle. Las alertas de CodeQL aparecen en **Security →
   Code scanning**.
2. Corregir en la misma rama. Si es un falso positivo, documentarlo: comentario en el PR y, en
   gitleaks, la huella exacta en `.gitleaksignore`.
3. Si se filtró un secreto real, primero **rotarlo** (quitarlo del código no basta) y después
   limpiar el historial con el coordinador del equipo.
