# Apache Kafka for the asynchronous events between microservices
# (FatigueDetected, AlertIssued, IncidentCreated...). MSK Serverless avoids
# sizing brokers and authenticates clients with IAM.
resource "aws_security_group" "kafka" {
  name        = "${var.name}-kafka"
  description = "Kafka reachable only from the EKS nodes"
  vpc_id      = var.vpc_id
  tags        = var.tags
}

resource "aws_vpc_security_group_ingress_rule" "kafka" {
  for_each = toset(var.allowed_security_group_ids)

  security_group_id            = aws_security_group.kafka.id
  referenced_security_group_id = each.value
  from_port                    = 9098
  to_port                      = 9098
  ip_protocol                  = "tcp"
}

resource "aws_msk_serverless_cluster" "events" {
  cluster_name = "${var.name}-events"

  vpc_config {
    subnet_ids         = var.private_subnet_ids
    security_group_ids = [aws_security_group.kafka.id]
  }

  client_authentication {
    sasl {
      iam {
        enabled = true
      }
    }
  }

  tags = var.tags
}
