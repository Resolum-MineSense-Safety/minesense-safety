# C3 – Vista de Componentes de MineSense Safety

Fuente: informe del equipo, sección 2.1.4.3 *Vista de Componentes (C4 – Nivel 3)*, complementada con 2.1.4.2 (contenedores) y 2.1.6 (modelo de datos, eventos y endpoints).

Se detallan tres contenedores: dos microservicios cloud (**Fatigue Detection** e **Incident Management**) y el **Edge Processing Service**, por ser el elemento crítico de los QAS 01 y 02.

Los microservicios .NET siguen una **estructura hexagonal**:

| Capa | Rol |
|------|-----|
| Adaptadores de entrada | Controladores y consumidores |
| Aplicación | Casos de uso |
| Dominio | Agregados y políticas |
| Salida | Puertos con adaptadores de salida |

> **Importante.** En el informe, los tres diagramas de componentes son imágenes y la sección 2.1.4.3 no incluye texto sobre los componentes individuales. Los diagramas de abajo aplican la estructura hexagonal descrita a las entidades, eventos y endpoints que el informe asigna a cada servicio en 2.1.6. **Los nombres de los componentes son descriptivos y no provienen del informe**; deben contrastarse con las imágenes originales o con `workspace.dsl` cuando esté disponible.

## Fatigue Detection Service

```mermaid
flowchart LR
    edge["Edge Processing Service<br/>[Contenedor Edge]"]
    kafka["Event Bus<br/>[Apache Kafka]"]
    mongo[("Fatigue Detection DB<br/>[MongoDB]")]

    subgraph fds["Fatigue Detection Service - C#, .NET 8"]
        subgraph inA["Adaptadores de entrada"]
            ctrl["Detections Controller<br/>POST /api/v1/detections<br/>GET /api/v1/detections/id"]
        end
        subgraph appA["Aplicación"]
            uc["Casos de uso<br/>Registrar detección, consultar detección"]
        end
        subgraph domA["Dominio"]
            agg["Agregados<br/>DetectionEvent, SignalWindow, RiskClassification"]
            pol["Políticas de clasificación del riesgo<br/>normal, alerta, crítico"]
        end
        subgraph outA["Puertos y adaptadores de salida"]
            repo["Detection Repository<br/>[Adaptador MongoDB]"]
            pub["Event Publisher<br/>[Adaptador Kafka]"]
        end
    end

    edge -- "Registra detección, REST /api/v1" --> ctrl
    ctrl --> uc
    uc --> agg
    uc --> pol
    uc --> repo
    uc --> pub
    repo -- "Lee y escribe" --> mongo
    pub -- "FatigueDetected, RiskLevelChanged" --> kafka
```

| Componente | Capa | Respaldo en el informe |
|------------|------|------------------------|
| Detections Controller | Entrada | Endpoints `POST /api/v1/detections` ("Registra una detección generada por el procesamiento Edge") y `GET /api/v1/detections/{id}` (2.1.6). |
| Casos de uso | Aplicación | Derivados de los endpoints anteriores. |
| Agregados `DetectionEvent`, `SignalWindow`, `RiskClassification` | Dominio | Entidades principales del servicio (2.1.6). |
| Políticas de clasificación | Dominio | Clasificación en normal, alerta o crítico (2.1.3). |
| Repositorio MongoDB | Salida | Persistencia MongoDB (2.1.6, ADR-005). |
| Publicador Kafka | Salida | Eventos `FatigueDetected` y `RiskLevelChanged` (2.1.6, ADR-004). |

## Incident Management Service

```mermaid
flowchart LR
    dashboard["Supervision Dashboard<br/>[React]"]
    kafka["Event Bus<br/>[Apache Kafka]"]
    mongo[("Incident Management DB<br/>[MongoDB]")]

    subgraph ims["Incident Management Service - C#, .NET 8"]
        subgraph inB["Adaptadores de entrada"]
            ictrl["Incidents Controller<br/>GET /api/v1/incidents<br/>GET /api/v1/incidents/id<br/>PATCH .../assign, PATCH .../close"]
            cons["Event Consumer<br/>[Adaptador Kafka]"]
        end
        subgraph appB["Aplicación"]
            iuc["Casos de uso<br/>Crear, listar, asignar, escalar y cerrar incidentes"]
        end
        subgraph domB["Dominio"]
            iagg["Agregado Incident<br/>IncidentAction, Assignment, Escalation, Closure"]
            ipol["Políticas del ciclo de vida<br/>recepción, asignación, respuesta, escalamiento, cierre"]
        end
        subgraph outB["Puertos y adaptadores de salida"]
            irepo["Incident Repository<br/>[Adaptador MongoDB]"]
            ipub["Event Publisher<br/>[Adaptador Kafka]"]
        end
    end

    dashboard -- "REST JSON /api/v1, Bearer JWT" --> ictrl
    kafka -- "Eventos de riesgo" --> cons
    ictrl --> iuc
    cons --> iuc
    iuc --> iagg
    iuc --> ipol
    iuc --> irepo
    iuc --> ipub
    irepo -- "Lee y escribe" --> mongo
    ipub -- "IncidentCreated, IncidentAssigned, IncidentClosed" --> kafka
```

| Componente | Capa | Respaldo en el informe |
|------------|------|------------------------|
| Incidents Controller | Entrada | Endpoints `GET /api/v1/incidents`, `GET /api/v1/incidents/{id}`, `PATCH /api/v1/incidents/{id}/assign`, `PATCH /api/v1/incidents/{id}/close` (2.1.6). |
| Event Consumer | Entrada | "Controladores y consumidores" como adaptadores de entrada (2.1.4.3). |
| Casos de uso | Aplicación | Ciclo de vida del incidente (2.1.3, EP05). |
| Agregado `Incident` | Dominio | Entidades `Incident`, `IncidentAction`, `Assignment`, `Escalation`, `Closure` (2.1.6). |
| Políticas del ciclo de vida | Dominio | Recepción, asignación, respuesta, escalamiento y cierre (2.1.3). |
| Repositorio MongoDB | Salida | Persistencia MongoDB (2.1.6). |
| Publicador Kafka | Salida | Eventos `IncidentCreated`, `IncidentAssigned`, `IncidentClosed` (2.1.6). |

## Edge Processing Service

```mermaid
flowchart LR
    devices["Wearable y cámara de cabina<br/>[Sistemas externos]"]
    broker["Edge MQTT Broker<br/>[Mosquitto, MQTTS]"]

    subgraph eps["Edge Processing Service - Python, scikit-learn"]
        ingest["Ingesta de señales"]
        norm["Normalización de señales"]
        infer["Inferencia del modelo de fatiga<br/>[scikit-learn]"]
        classify["Clasificación del riesgo<br/>normal, alerta, crítico"]
        publish["Publicador de detecciones<br/>[Cliente MQTT]"]
    end

    alert["Edge Alert Service"]
    sync["Edge Sync Agent"]

    devices -- "Señales" --> broker
    broker -- "Señales" --> ingest
    ingest --> norm
    norm --> infer
    infer --> classify
    classify --> publish
    publish -- "Detección y nivel de riesgo" --> broker
    broker --> alert
    broker --> sync
```

| Componente | Respaldo en el informe |
|------------|------------------------|
| Ingesta, normalización, inferencia y clasificación | Responsabilidad del contenedor: "Ingesta, normaliza, infiere y clasifica el riesgo" (2.1.4.2, QAS 02); scikit-learn y pytest (2.1.5, ADR-006). |
| Publicador MQTT | Los contenedores del gateway se comunican por el broker local (ADR-003). |
| Niveles de riesgo | Normal, alerta o crítico (2.1.3). |

## Ambigüedades

- Los nombres y la granularidad de los componentes son una reconstrucción (ver nota inicial).
- El informe no indica qué eventos consume el Incident Management Service para crear un incidente (podrían ser `FatigueDetected`, `RiskLevelChanged` o `AlertGenerated`); por eso el diagrama usa la etiqueta genérica "Eventos de riesgo".
- No se especifica si el Fatigue Detection Service consume eventos de Kafka además de recibir detecciones por REST; tampoco si recibe detecciones vía AWS IoT Core. Solo el endpoint `POST /api/v1/detections` está documentado.
- No se especifica si el Edge Processing Service recibe las señales de los dispositivos a través del broker local o directamente.
- La validación del JWT (ADR-007) se haría en los adaptadores de entrada de cada servicio, pero el informe no la muestra como componente.
