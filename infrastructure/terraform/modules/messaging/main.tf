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
  for_each = { for index, id in var.allowed_security_group_ids : tostring(index) => id }

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

# SASL/IAM endpoint (port 9098) the services receive as Kafka__BootstrapServers (ADR-004).
data "aws_msk_bootstrap_brokers" "events" {
  cluster_arn = aws_msk_serverless_cluster.events.arn
}

# ---------- IAM role of the services that use Kafka (IRSA) ----------
# MSK Serverless only accepts SASL/IAM: without this role every producer and consumer fails to
# authenticate. One shared role because all of them need the same data-plane actions on one cluster.
data "aws_iam_policy_document" "kafka_client_assume" {
  statement {
    actions = ["sts:AssumeRoleWithWebIdentity"]

    principals {
      type        = "Federated"
      identifiers = [var.oidc_provider_arn]
    }

    condition {
      test     = "StringEquals"
      variable = "${var.oidc_provider}:aud"
      values   = ["sts.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "${var.oidc_provider}:sub"
      values   = [for account in var.client_service_accounts : "system:serviceaccount:${var.kubernetes_namespace}:${account}"]
    }
  }
}

resource "aws_iam_role" "kafka_client" {
  name               = "${var.name}-kafka-client"
  assume_role_policy = data.aws_iam_policy_document.kafka_client_assume.json
  tags               = var.tags
}

data "aws_iam_policy_document" "kafka_client_access" {
  statement {
    sid = "Cluster"
    actions = [
      "kafka-cluster:Connect",
      "kafka-cluster:DescribeCluster",
      # The producers run with enable.idempotence.
      "kafka-cluster:WriteDataIdempotently",
    ]
    resources = [aws_msk_serverless_cluster.events.arn]
  }

  statement {
    sid = "Topics"
    actions = [
      # The consumers create their topics: MSK Serverless has no auto-creation.
      "kafka-cluster:CreateTopic",
      "kafka-cluster:DescribeTopic",
      "kafka-cluster:WriteData",
      "kafka-cluster:ReadData",
    ]
    resources = ["${replace(aws_msk_serverless_cluster.events.arn, ":cluster/", ":topic/")}/*"]
  }

  statement {
    sid       = "ConsumerGroups"
    actions   = ["kafka-cluster:AlterGroup", "kafka-cluster:DescribeGroup"]
    resources = ["${replace(aws_msk_serverless_cluster.events.arn, ":cluster/", ":group/")}/*"]
  }
}

resource "aws_iam_role_policy" "kafka_client_access" {
  name   = "kafka-data-plane"
  role   = aws_iam_role.kafka_client.id
  policy = data.aws_iam_policy_document.kafka_client_access.json
}
