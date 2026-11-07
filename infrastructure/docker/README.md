# Contenedorización (sección 3.1)

![Contenedorización](https://raw.githubusercontent.com/Resolum-MineSense-Safety/minesense-safety/develop/docs/diagrams/containerization.png)

Cada microservicio de MineSense Safety se empaqueta en su propia imagen OCI y todas siguen la
misma plantilla. Con esas imágenes se levanta el entorno local completo (Docker Compose), las
verifica el CI y se despliegan en Amazon EKS (sección 3.4).

## Imágenes

| Imagen | Origen | Base de runtime | Puerto |
| --- | --- | --- | --- |
| 13 microservicios .NET 8 | `src/backend/<Servicio>/Dockerfile` | `mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled-extra` | 8080 |
| `supervision-dashboard` | `src/frontend/Dockerfile` | `nginxinc/nginx-unprivileged:1.30.6-alpine3.24` | 8080 |
| `gateway` (solo local) | `infrastructure/docker/gateway/nginx.conf` | `nginxinc/nginx-unprivileged:1.30.6-alpine3.24` | 8080 |

### Plantilla de los microservicios

1. **Etapa `build` (`sdk:8.0`)**: copia primero solo los `.csproj` de `Shared` y del servicio y
   ejecuta `dotnet restore`. Esa capa queda en caché mientras no cambien las dependencias.
   Después copia el código y ejecuta `dotnet publish -c Release /p:UseAppHost=false`.
2. **Etapa `runtime` (`aspnet:8.0-jammy-chiseled-extra`)**: solo recibe `/app/publish`. Es una
   imagen Ubuntu *chiseled*, sin shell, sin gestor de paquetes y con el usuario no root `app`
   (UID 1654). La variante `extra` conserva ICU y tzdata, de modo que las tildes, las culturas
   y las zonas horarias se comportan igual que en desarrollo.
3. Escucha en el puerto 8080 (`ASPNETCORE_HTTP_PORTS`) y lleva etiquetas OCI de origen, título
   y proveedor.

El contexto de construcción es `src/backend`, para poder copiar el shared kernel.
`src/backend/.dockerignore` excluye `bin/`, `obj/`, `TestResults` y la cobertura.

```bash
docker build -f src/backend/OperationsService/Dockerfile -t minesense/operations-service src/backend
```

### Comparación de la imagen base (Operations Service)

| | `aspnet:8.0` (Debian 12) | `aspnet:8.0-jammy-chiseled-extra` |
| --- | --- | --- |
| Tamaño comprimido | 91 MB | **65 MB (−28 %)** |
| Vulnerabilidades (Trivy 0.56.2) | 243: 1 crítica, 58 altas, 95 medias, 87 bajas, 2 sin clasificar | **6: 2 medias, 4 bajas** |
| Shell en el contenedor | sí (`/bin/sh`) | no |
| Usuario | 1654 | 1654 |
| Memoria en reposo | ~34 MiB | ~32 MiB |

## Entorno local completo

```bash
cp .env.example .env              # opcional: credenciales locales
docker compose up --build -d      # 13 microservicios + dashboard + gateway + PostgreSQL, MongoDB, Mosquitto
docker compose ps
docker compose logs -f operations-service
docker compose down               # agregar -v para borrar los volúmenes
```

- **Entrada única:** `http://localhost:8080` sirve el Dashboard y `http://localhost:8080/api/v1/...`
  la API, con las mismas rutas que el balanceador de AWS.
- **Acceso directo:** cada servicio publica también su puerto propio para Swagger
  (`http://localhost:52xx/swagger`):

| Servicio | Puerto | Servicio | Puerto |
| --- | --- | --- | --- |
| monitoring | 5201 | incident-management | 5208 |
| identity-access | 5202 | analytics | 5209 |
| operations | 5203 | privacy-audit | 5260 |
| device-management | 5204 | reporting | 5270 |
| fatigue-detection | 5205 | edge-synchronization | 5280 |
| alert | 5206 | feedback-reliability | 5281 |
| fleet-monitoring | 5207 | gateway (API + Dashboard) | 8080 |

Todos los contenedores .NET heredan el bloque `x-dotnet-service`: `read_only: true`, `/tmp` en
memoria, `cap_drop: ALL` y `no-new-privileges`. Son las mismas restricciones que el
`securityContext` del chart de Kubernetes, así que un problema de permisos aparece primero en
local y no en el clúster.

### Gateway local

`infrastructure/docker/gateway/nginx.conf` se **genera** a partir de las rutas del chart
`minesense-gateway`, para que el entorno local y el ALB de la nube enruten igual:

```bash
python infrastructure/scripts/render-gateway-nginx.py
```

El workflow `containers-ci.yml` falla si el archivo commiteado no coincide con el generado.

## Verificación continua

`.github/workflows/containers-ci.yml` se ejecuta en cada Pull Request que toca el código, los
Dockerfiles o el compose:

1. `docker compose config`, regeneración del gateway y `nginx -t`.
2. Para cada una de las 14 imágenes: build con caché de capas, arranque con
   `--read-only --tmpfs /tmp --cap-drop ALL --security-opt no-new-privileges`, consulta del
   probe de salud, verificación de que el usuario no es root, escaneo con Trivy (falla ante
   vulnerabilidades críticas o altas con corrección disponible) y tamaño de la imagen en el
   resumen del job.
