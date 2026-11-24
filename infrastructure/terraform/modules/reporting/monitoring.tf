# Operational monitoring of the serverless report flow (section 3.5, AV3 feedback):
# CloudWatch alarms on the Lambda and on both queues, an SNS topic that notifies the team and a
# dashboard that shows, in one screen, whether reports are being produced and how fast.
#
# What each alarm protects:
#   errors         - the Lambda failed at least once: a supervisor's PDF did not come out on the first try
#   throttles      - account concurrency reached: requests wait in the queue
#   duration p95   - renders close to the timeout: the next bigger report will fail
#   queue age      - the oldest request waits longer than the threshold: the Lambda is not consuming
#   dlq messages   - a request failed three times and was discarded: the Reporting Service marks it Failed

locals {
  lambda_name   = aws_lambda_function.report_pdf_generator.function_name
  alarm_actions = var.alarm_email == "" ? [] : [aws_sns_topic.alarms.arn]
}

resource "aws_sns_topic" "alarms" {
  name              = "${var.name}-report-alarms"
  kms_master_key_id = "alias/aws/sns"
  tags              = var.tags
}

resource "aws_sns_topic_subscription" "alarm_email" {
  count     = var.alarm_email == "" ? 0 : 1
  topic_arn = aws_sns_topic.alarms.arn
  protocol  = "email"
  endpoint  = var.alarm_email
}

resource "aws_cloudwatch_metric_alarm" "lambda_errors" {
  alarm_name          = "${local.lambda_name}-errors"
  alarm_description   = "ReportPdfGenerator failed at least once in 5 minutes."
  namespace           = "AWS/Lambda"
  metric_name         = "Errors"
  dimensions          = { FunctionName = local.lambda_name }
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 1
  threshold           = 0
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions
  tags                = var.tags
}

resource "aws_cloudwatch_metric_alarm" "lambda_throttles" {
  alarm_name          = "${local.lambda_name}-throttles"
  alarm_description   = "ReportPdfGenerator was throttled: report requests are waiting for concurrency."
  namespace           = "AWS/Lambda"
  metric_name         = "Throttles"
  dimensions          = { FunctionName = local.lambda_name }
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 1
  threshold           = 0
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  tags                = var.tags
}

resource "aws_cloudwatch_metric_alarm" "lambda_duration" {
  alarm_name          = "${local.lambda_name}-duration-p95"
  alarm_description   = "p95 render time above ${var.duration_alarm_ratio * 100}% of the Lambda timeout."
  namespace           = "AWS/Lambda"
  metric_name         = "Duration"
  dimensions          = { FunctionName = local.lambda_name }
  extended_statistic  = "p95"
  period              = 300
  evaluation_periods  = 2
  threshold           = var.lambda_timeout_seconds * 1000 * var.duration_alarm_ratio
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  tags                = var.tags
}

resource "aws_cloudwatch_metric_alarm" "queue_age" {
  alarm_name          = "${aws_sqs_queue.requests.name}-oldest-message-age"
  alarm_description   = "A report request has waited more than ${var.queue_age_alarm_seconds} s in the queue."
  namespace           = "AWS/SQS"
  metric_name         = "ApproximateAgeOfOldestMessage"
  dimensions          = { QueueName = aws_sqs_queue.requests.name }
  statistic           = "Maximum"
  period              = 300
  evaluation_periods  = 1
  threshold           = var.queue_age_alarm_seconds
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  tags                = var.tags
}

resource "aws_cloudwatch_metric_alarm" "dlq_messages" {
  alarm_name          = "${aws_sqs_queue.requests_dlq.name}-not-empty"
  alarm_description   = "A report request failed three times and is in the dead-letter queue."
  namespace           = "AWS/SQS"
  metric_name         = "ApproximateNumberOfMessagesVisible"
  dimensions          = { QueueName = aws_sqs_queue.requests_dlq.name }
  statistic           = "Maximum"
  period              = 300
  evaluation_periods  = 1
  threshold           = 0
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  tags                = var.tags
}

data "aws_region" "current" {}

resource "aws_cloudwatch_dashboard" "reports" {
  dashboard_name = "${var.name}-report-generation"

  dashboard_body = jsonencode({
    widgets = [
      {
        type = "metric", x = 0, y = 0, width = 12, height = 6
        properties = {
          title  = "ReportPdfGenerator: invocaciones, errores y throttles"
          region = data.aws_region.current.name
          stat   = "Sum"
          period = 300
          metrics = [
            ["AWS/Lambda", "Invocations", "FunctionName", local.lambda_name],
            [".", "Errors", ".", "."],
            [".", "Throttles", ".", "."],
          ]
        }
      },
      {
        type = "metric", x = 12, y = 0, width = 12, height = 6
        properties = {
          title  = "ReportPdfGenerator: duración (ms)"
          region = data.aws_region.current.name
          period = 300
          metrics = [
            ["AWS/Lambda", "Duration", "FunctionName", local.lambda_name, { stat = "p50" }],
            ["...", { stat = "p95" }],
            ["...", { stat = "Maximum" }],
          ]
          annotations = {
            horizontal = [{ label = "timeout", value = var.lambda_timeout_seconds * 1000 }]
          }
        }
      },
      {
        type = "metric", x = 0, y = 6, width = 12, height = 6
        properties = {
          title  = "Cola report-requests: enviados, recibidos y antigüedad (s)"
          region = data.aws_region.current.name
          period = 300
          metrics = [
            ["AWS/SQS", "NumberOfMessagesSent", "QueueName", aws_sqs_queue.requests.name, { stat = "Sum" }],
            [".", "NumberOfMessagesDeleted", ".", ".", { stat = "Sum" }],
            [".", "ApproximateAgeOfOldestMessage", ".", ".", { stat = "Maximum", yAxis = "right" }],
          ]
        }
      },
      {
        type = "metric", x = 12, y = 6, width = 12, height = 6
        properties = {
          title  = "Dead-letter queue: solicitudes descartadas"
          region = data.aws_region.current.name
          stat   = "Maximum"
          period = 300
          metrics = [
            ["AWS/SQS", "ApproximateNumberOfMessagesVisible", "QueueName", aws_sqs_queue.requests_dlq.name],
          ]
        }
      },
      {
        type = "alarm", x = 0, y = 12, width = 24, height = 3
        properties = {
          title = "Estado de las alarmas del flujo de reportes"
          alarms = [
            aws_cloudwatch_metric_alarm.lambda_errors.arn,
            aws_cloudwatch_metric_alarm.lambda_throttles.arn,
            aws_cloudwatch_metric_alarm.lambda_duration.arn,
            aws_cloudwatch_metric_alarm.queue_age.arn,
            aws_cloudwatch_metric_alarm.dlq_messages.arn,
          ]
        }
      },
    ]
  })
}
