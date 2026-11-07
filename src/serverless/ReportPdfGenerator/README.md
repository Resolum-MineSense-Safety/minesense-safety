# ReportPdfGenerator (AWS Lambda)

Función serverless de MineSense Safety (sección 3.5 del informe, ADR-008). Genera los
reportes PDF de cumplimiento (EP10) fuera de los microservicios para no bloquearlos.

```
Reporting Service (EKS) --SendMessage--> SQS report-requests --> Lambda --> S3 reports/yyyy/MM/{id}.pdf
                                                   └── 3 fallos --> SQS DLQ
```

- **Entrada**: mensaje JSON `ReportRequest` (periodo, ámbito, resumen e indicadores ya consolidados).
- **Salida**: PDF A4 en el bucket `REPORTS_BUCKET` (cifrado KMS), clave `reports/yyyy/MM/{reportId}.pdf`.
- **Errores**: las solicitudes inválidas se descartan con log; las fallas transitorias se
  devuelven con `ReportBatchItemFailures` y, tras 3 intentos, van a la DLQ.
- **Runtime**: .NET 8 (`dotnet8`) sobre arm64; PDF generado sin dependencias nativas.

La infraestructura (cola, DLQ, bucket, rol IAM y la propia función) está en
`infrastructure/terraform/modules/reporting`.

```bash
dotnet test src/serverless/ReportPdfGenerator.Tests
dotnet tool install -g Amazon.Lambda.Tools
dotnet lambda package -pl src/serverless/ReportPdfGenerator -o src/serverless/ReportPdfGenerator/artifacts/ReportPdfGenerator.zip
```

Ejemplo de mensaje en `sample-request.json`.
