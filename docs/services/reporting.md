# Reporting Service

Reporting Service implements EP10 (US22, TS-21). It keeps the versioned catalog of indicators
(`ReportDefinition`), the asynchronous report jobs (`ReportJob`) and the downloadable artifacts
(`ReportArtifact`, with SHA-256 checksum and a 7-day expiration).

## API routes

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/v1/report-definitions` | Define an indicator version. |
| `GET` | `/api/v1/report-definitions` | List indicators. |
| `POST` | `/api/v1/reports/safety` | Request a report (201 with the job). |
| `GET` | `/api/v1/reports`, `/api/v1/reports/{id}` | List jobs, job detail. |
| `GET` | `/api/v1/reports/{id}/download` | The file; 409 if not ready, 410 if expired or removed. |

## How a report is generated

The job goes `Requested -> Rendering -> Ready | Failed`. Which path it takes depends on the configuration.

**Not configured (local, tests, demo).** The job is rendered in process during the request and answers
`Ready`, with the bytes kept in the artifact.

**Configured (cloud, ADR-008, section 3.5).** PDFs go through the `ReportPdfGenerator` Lambda:

```
POST /reports/safety --> job Rendering --SendMessage--> SQS report-requests --> Lambda --> S3 reports/yyyy/MM/{id}.pdf
                                                                                              ^
ReportCompletionPoller, every 15 s: is the object there? --> artifact + job Ready ------------'
                                    not after 40 min?    --> job Failed
```

- The answer is `201` with status `Rendering`; clients poll the job (the dashboard does every 15 s).
- The message is the Lambda's `ReportRequest`. Its `requestedAt` fixes the key, so the service finds the
  PDF knowing only the job. A contract test checks message and key against the Lambda's own classes.
- `Ready`: the artifact records the key and the checksum and holds no bytes.
- Download streams the object through the service. A presigned URL is not used because the dashboard
  fetches this route with its token and the bucket has no CORS.
- `Failed`: the queue rejected the message (immediately) or the PDF did not appear in time. The timeout also
  covers what the service cannot see: messages the Lambda discards and those that end in the dead-letter
  queue. It defaults to 40 minutes because the queue allows three attempts of 12 minutes each.
- CSV stays in process: the Lambda only produces PDFs.
- A period with the same start and end answers 422, because the Lambda would discard it.
- Several replicas can poll at once: the artifact is unique per job.

The service does not consume the producer contexts yet, so it sends no consolidated figures: the PDF
prints "Sin datos" in the summary, never a zero.

## Configuration

| Key | Default | Purpose |
|---|---|---|
| `Reporting:SqsQueueUrl` | none | Queue URL (Terraform output `report_requests_queue_url`). |
| `Reporting:S3Bucket` | none | Bucket (Terraform output `reports_bucket`). |
| `Reporting:GenerationTimeoutMinutes` | `40` | Time before a job without PDF fails. |
| `Reporting:PollIntervalSeconds` | `15` | Time between checks. |
| `ConnectionStrings:MongoDb` | none | MongoDB (ADR-005); in memory without it. |

Set the queue and the bucket together: only one of them stops the service at startup. `deploy.sh` passes
them from `REPORT_REQUESTS_QUEUE_URL` and `REPORTS_BUCKET`. AWS credentials and region come from the pod
(IRSA, role `reporting_service_role_arn`), whose policy allows `sqs:SendMessage` on the queue,
`s3:GetObject` on `reports/*` and `s3:ListBucket` on the bucket (without it S3 answers 403 instead of 404
while the PDF does not exist).

If the flow is turned off later, jobs still in `Rendering` stay there and stored artifacts answer 410.

## Verification

```powershell
dotnet test src/backend/tests/ReportingService.Tests/ReportingService.Tests.csproj
dotnet test src/serverless/ReportPdfGenerator.Tests/ReportPdfGenerator.Tests.csproj
```
