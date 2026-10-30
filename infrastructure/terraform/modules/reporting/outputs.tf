output "reports_bucket" {
  value = aws_s3_bucket.reports.bucket
}

output "report_requests_queue_url" {
  value = aws_sqs_queue.requests.url
}

output "lambda_function_name" {
  value = aws_lambda_function.report_pdf_generator.function_name
}

output "reporting_service_role_arn" {
  description = "Annotate the reporting-service ServiceAccount with this role."
  value       = aws_iam_role.reporting_service.arn
}
