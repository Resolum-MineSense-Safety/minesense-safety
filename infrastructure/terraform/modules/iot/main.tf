# Cloud MQTT broker (AWS IoT Core over MQTTS). Every cabin gateway is a
# thing with its own X.509 certificate (issued at provisioning time, outside
# Terraform) and may only publish and subscribe under its own topic.
data "aws_region" "current" {}
data "aws_caller_identity" "current" {}

locals {
  arn_prefix = "arn:aws:iot:${data.aws_region.current.name}:${data.aws_caller_identity.current.account_id}"
  own_topic  = "${var.topic_root}/$${iot:Connection.Thing.ThingName}"
}

resource "aws_iot_thing_type" "gateway" {
  name = "${var.name}-edge-gateway"

  properties {
    description           = "MineSense Edge gateway installed in a haul truck cabin"
    searchable_attributes = ["vehicleId", "mineSite"]
  }

  tags = var.tags
}

resource "aws_iot_policy" "gateway" {
  name = "${var.name}-edge-gateway"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["iot:Connect"]
        Resource = ["${local.arn_prefix}:client/$${iot:Connection.Thing.ThingName}"]
      },
      {
        Effect   = "Allow"
        Action   = ["iot:Publish"]
        Resource = ["${local.arn_prefix}:topic/${local.own_topic}/*"]
      },
      {
        Effect   = "Allow"
        Action   = ["iot:Subscribe"]
        Resource = ["${local.arn_prefix}:topicfilter/${local.own_topic}/commands/*"]
      },
      {
        Effect   = "Allow"
        Action   = ["iot:Receive"]
        Resource = ["${local.arn_prefix}:topic/${local.own_topic}/commands/*"]
      }
    ]
  })
}

data "aws_iot_endpoint" "data" {
  endpoint_type = "iot:Data-ATS"
}
