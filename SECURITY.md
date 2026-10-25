# Política de seguridad

MineSense Safety procesa datos biométricos de operadores mineros (Ley N.° 29733). Las
vulnerabilidades se reportan y corrigen con prioridad sobre el resto del backlog.

## Reportar una vulnerabilidad

- **No abras un issue público.** Usa *Report a vulnerability* en la pestaña **Security** del
  repositorio (GitHub Private Vulnerability Reporting) o escribe al coordinador del equipo.
- Incluye el servicio afectado, los pasos para reproducirla y el impacto que observaste.
- Respondemos en un máximo de 3 días hábiles y acordamos contigo la fecha de divulgación.

## Versiones soportadas

| Rama | Soporte |
| --- | --- |
| `main` (Production) | Correcciones de seguridad |
| `develop` | Correcciones de seguridad y mejoras |
| Ramas `feature/*` antiguas | Sin soporte |

## Controles automáticos

Cada Pull Request ejecuta los controles descritos en
[docs/devsecops.md](https://github.com/Resolum-MineSense-Safety/minesense-safety/blob/develop/docs/devsecops.md):
análisis estático (SonarCloud y CodeQL), detección de secretos (gitleaks), revisión de
dependencias, escaneo de imágenes (Trivy) y validación de la infraestructura (Terraform y Helm).
