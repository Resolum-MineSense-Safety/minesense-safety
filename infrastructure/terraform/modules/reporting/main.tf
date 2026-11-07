# Serverless report generation (section 3.5, ADR-008): the Reporting Service
# enqueues a request, the Lambda renders the PDF and stores it in S3, so heavy
# work never blocks the microservices running in EKS.

# ---------- Storage of generated reports ----------
resource "aws_s3_bucket" "reports" {
  bucket_prefix = "${var.name}-reports-"
  tags          = var.tags
}

resource "aws_s3_bucket_public_access_block" "reports" {
  bucket                  = aws_s3_bucket.reports.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_server_side_encryption_configuration" "reports" {
  bucket = aws_s3_bucket.reports.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "aws:kms"
    }
  }
}

resource "aws_s3_bucket_versioning" "reports" {
  bucket = aws_s3_bucket.reports.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_lifecycle_configuration" "reports" {
  bucket = aws_s3_bucket.reports.id

  rule {
    id     = "expire-old-reports"
    status = "Enabled"

    filter {
      prefix = "reports/"
    }

    expiration {
      days = var.report_retention_days
    }
  }
}

# ---------- Request queue with dead-letter queue ----------
resource "aws_sqs_queue" "requests_dlq" {
  name                      = "${var.name}-report-requests-dlq"
  message_retention_seconds = 1209600
  sqs_managed_sse_enabled   = true
  tags                      = var.tags
}

resource "aws_sqs_queue" "requests" {
  name                       = "${var.name}-report-requests"
  visibility_timeout_seconds = var.lambda_timeout_seconds * 6
  sqs_managed_sse_enabled    = true

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.requests_dlq.arn
    maxReceiveCount     = 3
  })

  tags = var.tags
}

# ---------- Lambda function ----------
data "aws_iam_policy_document" "lambda_assume" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "lambda" {
  name               = "${var.name}-report-pdf-generator"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume.json
  tags               = var.tags
}

resource "aws_iam_role_policy_attachment" "lambda_logs" {
  role       = aws_iam_role.lambda.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

data "aws_iam_policy_document" "lambda_access" {
  statement {
    sid       = "ConsumeRequests"
    actions   = ["sqs:ReceiveMessage", "sqs:DeleteMessage", "sqs:GetQueueAttributes"]
    resources = [aws_sqs_queue.requests.arn]
  }

  statement {
    sid       = "StoreReports"
    actions   = ["s3:PutObject"]
    resources = ["${aws_s3_bucket.reports.arn}/reports/*"]
  }
}

resource "aws_iam_role_policy" "lambda_access" {
  name   = "report-generation"
  role   = aws_iam_role.lambda.id
  policy = data.aws_iam_policy_document.lambda_access.json
}

resource "aws_cloudwatch_log_group" "lambda" {
  name              = "/aws/lambda/${var.name}-report-pdf-generator"
  retention_in_days = 30
  tags              = var.tags
}

resource "aws_lambda_function" "report_pdf_generator" {
  function_name = "${var.name}-report-pdf-generator"
  role          = aws_iam_role.lambda.arn
  runtime       = "dotnet8"
  architectures = ["arm64"]
  handler       = "ReportPdfGenerator::ReportPdfGenerator.Function::FunctionHandler"

  filename         = var.lambda_package_path
  source_code_hash = fileexists(var.lambda_package_path) ? filebase64sha256(var.lambda_package_path) : null

  memory_size = var.lambda_memory_mb
  timeout     = var.lambda_timeout_seconds

  environment {
    variables = {
      REPORTS_BUCKET = aws_s3_bucket.reports.bucket
    }
  }

  depends_on = [aws_cloudwatch_log_group.lambda]
  tags       = var.tags
}

resource "aws_lambda_event_source_mapping" "requests" {
  event_source_arn        = aws_sqs_queue.requests.arn
  function_name           = aws_lambda_function.report_pdf_generator.arn
  batch_size              = 5
  function_response_types = ["ReportBatchItemFailures"]
}

# ---------- IAM role for the Reporting Service pods (IRSA) ----------
data "aws_iam_policy_document" "reporting_service_assume" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [var.oidc_provider_arn]
    }

    condition {
      test     = "StringEquals"
      variable = "${var.oidc_provider}:sub"
      values   = ["system:serviceaccount:${var.kubernetes_namespace}:reporting-service"]
    }
  }
}

resource "aws_iam_role" "reporting_service" {
  name               = "${var.name}-reporting-service"
  assume_role_policy = data.aws_iam_policy_document.reporting_service_assume.json
  tags               = var.tags
}

data "aws_iam_policy_document" "reporting_service_access" {
  statement {
    sid       = "RequestReports"
    actions   = ["sqs:SendMessage"]
    resources = [aws_sqs_queue.requests.arn]
  }

  statement {
    sid       = "DownloadReports"
    actions   = ["s3:GetObject"]
    resources = ["${aws_s3_bucket.reports.arn}/reports/*"]
  }
}

resource "aws_iam_role_policy" "reporting_service_access" {
  name   = "report-requests"
  role   = aws_iam_role.reporting_service.id
  policy = data.aws_iam_policy_document.reporting_service_access.json
}
