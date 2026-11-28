# Demo en AWS EC2 (sustentación)

Entorno completo de MineSense Safety en **una sola instancia EC2** con `docker compose`: los 13
microservicios, Kafka, PostgreSQL, MongoDB, Mosquitto, el dashboard, el gateway y el
[simulador IoT de flota](../../src/iot-simulator/README.md). Es la alternativa barata a Staging en
EKS (unos 1.25 USD/h): una `t3.xlarge` cuesta unos 0.17 USD/h y se destruye al terminar.

| Recurso | Detalle |
|---|---|
| Instancia | `t3.xlarge` (4 vCPU, 16 GiB), Amazon Linux 2023, disco gp3 de 40 GB, IMDSv2 obligatorio |
| Red | VPC por defecto, IP elástica, security group `minesense-demo` que solo abre el puerto 80 |
| Arranque | `user-data.sh`: instala Docker, Compose y Buildx, clona el repositorio en la ref indicada, genera `.env` y levanta todo; un servicio systemd lo vuelve a levantar si la instancia se reinicia |
| Datos | El simulador carga la mina de demostración (Tambo Real, 8 cabinas) en el primer arranque |
| Credenciales | `deploy.sh` genera contraseñas propias de la demo en `.demo-credentials` (fuera de git): `admin` usa `ADMIN_PASSWORD`; `supervisor`, `jefe.seguridad` y `operador.rosa` usan `DEMO_PASSWORD` |

## Uso

```bash
aws login                                   # o aws sso login; región us-east-1 por defecto
infrastructure/demo-ec2/deploy.sh up v0.2.0 # o una rama: develop
infrastructure/demo-ec2/deploy.sh status
infrastructure/demo-ec2/deploy.sh logs      # log de arranque por la consola de EC2
infrastructure/demo-ec2/deploy.sh down      # termina la instancia y libera IP y security group
```

El primer arranque tarda de 15 a 25 minutos porque construye las imágenes en la instancia. Conviene
levantarla el día anterior a la exposición, revisar `http://<ip>` y bajarla después.

## Qué revisar antes de exponer

1. `http://<ip>` abre el login; entrar como `supervisor` con `DEMO_PASSWORD`.
2. **Telemetría IoT** muestra las cabinas transmitiendo y el contador de mensajes MQTT subiendo.
3. **Centro de control**: los mini-gráficos de PERCLOS se mueven y aparecen alertas cada pocos minutos.
4. **Servicios**: los 14 responden (13 microservicios y el gateway IoT).
