# Infrastructure as Code — Terraform

Infraestructura cloud de MineSense Safety en AWS, declarada con Terraform (ADR-008).
Staging y Production usan **los mismos módulos** y difieren solo en tamaño y
redundancia, de modo que lo validado en Staging es lo que se promueve a Production.

## Estructura

```
infrastructure/terraform/
├── bootstrap/                 # Estado remoto (S3 + DynamoDB) y proveedor OIDC de GitHub (se aplica una vez)
├── modules/
│   ├── network/               # VPC, subredes públicas/privadas en 2-3 AZ, NAT
│   ├── eks/                   # Clúster Amazon EKS + node group administrado + IRSA
│   ├── data/                  # RDS PostgreSQL 16 y Amazon DocumentDB (Database per Service)
│   ├── messaging/             # Amazon MSK Serverless (Kafka) con autenticación IAM
│   ├── iot/                   # AWS IoT Core: tipo de thing y política MQTTS de los gateways Edge
│   ├── reporting/             # Lambda de reportes PDF + SQS (con DLQ) + bucket S3 (sección 3.5)
│   └── delivery/              # Rol de despliegue de GitHub Actions (OIDC) + acceso a EKS + rol del Load Balancer Controller (sección 3.4)
└── environments/
    ├── staging/               # 2 AZ, 1 NAT, t3.large x2, sin Multi-AZ
    └── production/            # 3 AZ, NAT por AZ, m6i.large x3, Multi-AZ y protección de borrado
```

| Recurso | Staging | Production |
| --- | --- | --- |
| VPC | 10.20.0.0/16, 2 AZ | 10.30.0.0/16, 3 AZ |
| NAT Gateway | 1 compartido | 1 por AZ |
| Nodos EKS | t3.large, 2–3 | m6i.large, 3–6 |
| PostgreSQL | db.t4g.micro, Single-AZ | db.t4g.medium, Multi-AZ |
| DocumentDB | 1 instancia | 2 instancias |
| Backups | 3 días | 14 días |
| Protección de borrado | No | Sí |

## Uso

```bash
# 1. Una sola vez por cuenta: estado remoto
cd infrastructure/terraform/bootstrap
terraform init && terraform apply

# 2. Empaquetar la Lambda (lo hace el pipeline de CD)
dotnet lambda package -pl src/serverless/ReportPdfGenerator -o src/serverless/ReportPdfGenerator/artifacts/ReportPdfGenerator.zip

# 3. Aprovisionar un entorno
cd infrastructure/terraform/environments/staging
terraform init
terraform plan -out tfplan
terraform apply tfplan
```

## Decisiones

- **Imágenes en GitHub Container Registry** (`ghcr.io/resolum-minesense-safety`), como define la
  estrategia de entornos (2.2.4): se construyen una vez y se promueven de Staging a Production;
  Terraform no crea un registro propio.
- **Estado remoto cifrado y con bloqueo** (S3 + DynamoDB): dos integrantes no pueden aplicar a la vez.
- **Credenciales fuera del repositorio**: RDS gestiona su contraseña en Secrets Manager
  (`manage_master_user_password`); la de DocumentDB se genera y guarda en Secrets Manager.
- **Red privada**: bases de datos, Kafka y nodos EKS viven en subredes privadas y solo aceptan
  tráfico del security group de los nodos.
- **Mínimo privilegio**: la Lambda solo puede leer su cola y escribir en `reports/`; el
  Reporting Service obtiene su rol IAM vía IRSA (ServiceAccount `reporting-service`).
- **Gateways Edge**: cada cabina es un *thing* de IoT Core con certificado X.509 propio y
  solo puede publicar bajo `minesense/<thingName>/…`.
- `*.tfvars` y `*.tfstate` están en `.gitignore`; los valores por defecto viven en `variables.tf`.

El workflow `.github/workflows/terraform-validate.yml` ejecuta `terraform fmt -check` y
`terraform validate` en cada Pull Request que toque `infrastructure/terraform/`.

El despliegue de los microservicios sobre estos clústeres (Helm, Ingress y pipeline de CD) se
documenta en [`infrastructure/helm/README.md`](../helm/README.md).
