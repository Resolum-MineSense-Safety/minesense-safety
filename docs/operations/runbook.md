# Runbook de respuesta ante incidentes

Sección 4.3 del informe. Cada alerta de Prometheus enlaza a la sección de este archivo que le corresponde.
Los permisos de `kubectl` necesarios están en el rol `minesense-oncall`
([`infrastructure/k8s/rbac.yaml`](../../infrastructure/k8s/rbac.yaml)).

## Proceso general

1. **Reconocer** la alerta en el canal antes de 5 minutos (crítica) o 30 minutos (advertencia) y avisar «estoy en esto».
2. **Evaluar el impacto.** Lo más importante es saber si se perdieron alertas de fatiga. Revisar el panel
   «SLO-2» y la pantalla Edge del dashboard. El agente Edge sigue alertando en cabina aunque la nube falle
   (QAS 01), así que el riesgo está en la visibilidad del supervisor.
3. **Mitigar primero y diagnosticar después.** Hacer rollback, reiniciar o escalar, según la sección de abajo.
4. **Comunicar** el estado cada 30 minutos mientras dure el incidente.
5. **Postmortem sin culpables** antes de 48 h, con línea de tiempo, causa raíz, acciones y responsables.
   Se archiva en `docs/operations/postmortems/AAAA-MM-DD-<titulo>.md` y se mide en el *time to restore*
   (DORA, sección 4.6).

Variables usadas abajo: `NS=staging` (o `production`) y `SVC=<nombre del servicio>`.

## RB-01 Servicio caído

**Alerta:** `MineSenseServiceDown`. Prometheus no puede raspar el servicio desde hace 2 minutos.

```bash
kubectl -n $NS get pods -l app.kubernetes.io/name=$SVC
kubectl -n $NS describe pod -l app.kubernetes.io/name=$SVC | tail -30
kubectl -n $NS logs deploy/$SVC --tail=100
```

- Si los pods están en `Pending`, falta capacidad: ver la cuota con `kubectl -n $NS describe resourcequota` y los nodos con `kubectl get nodes`.
- Si están en `ImagePullBackOff`, el tag no existe o el Secret `ghcr-pull` expiró. Volver a correr `create-k8s-secrets.sh`.
- Si están en `Running` pero sin pasar el *readiness*, la base de datos o Kafka no responden. Seguir con RB-02.

## RB-02 Tasa de errores 5xx

**Alertas:** `MineSenseErrorBudgetFastBurn`, `MineSenseErrorBudgetSlowBurn`.

1. ¿Coincide con un despliegue? Revisar con `helm history $SVC -n $NS`. Si la última revisión es de las
   últimas 2 horas, **hacer rollback de inmediato**:
   ```bash
   helm rollback $SVC 0 -n $NS --wait     # 0 = revisión anterior
   ```
   o volver a lanzar `cd-deploy.yml` con `image_tag` de la versión estable (sección 4.2).
2. Si no hubo despliegue, buscar la excepción en CloudWatch Logs Insights
   (`/aws/containerinsights/<cluster>/application`):
   ```
   fields @timestamp, kubernetes.pod_name, log
   | filter kubernetes.container_name = "$SVC" and log like /fail|Exception/
   | sort @timestamp desc | limit 50
   ```
3. Si el error es de dependencia (DocumentDB, RDS o MSK), revisar el estado en la consola de AWS. Mientras
   tanto el servicio responde 503 y el agente Edge guarda los lotes en su buffer (US13/US14).

## RB-03 Latencia alta

**Alertas:** `MineSenseCriticalPathSlow` (P95 > 500 ms en detección o alertas), `MineSenseApiSlow` (P95 > 2 s).

```bash
kubectl -n $NS get hpa $SVC
kubectl -n $NS top pods -l app.kubernetes.io/name=$SVC
```

- Si el HPA no llegó a su máximo y la CPU está alta, el HPA está escalando. Esperar 2 minutos y confirmar.
- Si llegó a su máximo, seguir con RB-06.
- Si la CPU está baja pero la latencia es alta, la causa está en la base de datos. Revisar las métricas
  de DocumentDB o RDS en CloudWatch.

## RB-04 Pico de accesos no autorizados

**Alerta:** `MineSenseUnauthorizedSpike` (más de 5 rechazos 401/403 por segundo).

1. Ver el origen en los logs del servicio (mensaje `Access rejected 401 <método> <ruta> at <fecha> for <usuario>: <motivo>`, que exige QAS 04) y en los logs de acceso del ALB.
2. Si es un cliente propio con un token vencido (por ejemplo, el agente Edge), reiniciarlo.
3. Si es tráfico externo, bloquear la IP en AWS WAF (`wafAclArn` del gateway) y avisar al responsable de
   seguridad. Registrar el incidente en Privacy & Audit (`/api/v1/audit`).

## RB-05 Pod en CrashLoopBackOff

**Alerta:** `MineSensePodCrashLooping`.

```bash
kubectl -n $NS logs deploy/$SVC --previous --tail=100
```

Si empezó con un despliegue, hacer rollback (RB-02, paso 1). Si es de configuración (Secret o ConfigMap),
corregirla y aplicar `kubectl -n $NS rollout restart deploy/$SVC`.

## RB-06 HPA en su máximo

**Alerta:** `MineSenseHpaAtMaximum` (15 minutos con todas las réplicas).

1. Escalar manualmente por encima del máximo como medida temporal (rol `minesense-oncall`):
   ```bash
   kubectl -n $NS patch hpa $SVC -p '{"spec":{"maxReplicas":6}}'
   ```
2. Si los nodos no alcanzan, subir `node_desired_size` en Terraform. Requiere PR y aprobación.
3. Registrar en el postmortem el nuevo valor de `maxReplicas` en `values/environments/$NS.yaml`, para que
   el siguiente despliegue no lo revierta.

## Contactos y escalamiento

| Rol | Quién | Escala a |
|---|---|---|
| Guardia (on-call) | Rotación semanal entre los 8 integrantes; el turno se publica en el tablero del sprint | Responsable de arquitectura |
| Responsable de arquitectura | Dueño de los ADR | Scrum Master |
| Responsable de seguridad | Dueño de `security-scan.yml` y de `SECURITY.md` | Product Owner |
