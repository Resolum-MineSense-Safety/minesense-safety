# Database per Service (ADR on data): PostgreSQL for Identity, Operations,
# Device and Monitoring; a MongoDB-compatible cluster (Amazon DocumentDB)
# for telemetry, alerts, incidents, reporting and analytics. Each service
# gets its own database/schema inside these engines; credentials live in
# Secrets Manager and never in the repository.

resource "aws_db_subnet_group" "postgres" {
  name       = "${var.name}-postgres"
  subnet_ids = var.private_subnet_ids
  tags       = var.tags
}

resource "aws_security_group" "postgres" {
  name        = "${var.name}-postgres"
  description = "PostgreSQL reachable only from the EKS nodes"
  vpc_id      = var.vpc_id
  tags        = var.tags
}

resource "aws_vpc_security_group_ingress_rule" "postgres" {
  for_each = toset(var.allowed_security_group_ids)

  security_group_id            = aws_security_group.postgres.id
  referenced_security_group_id = each.value
  from_port                    = 5432
  to_port                      = 5432
  ip_protocol                  = "tcp"
}

resource "aws_db_instance" "postgres" {
  identifier     = "${var.name}-postgres"
  engine         = "postgres"
  engine_version = "16"
  instance_class = var.postgres_instance_class

  allocated_storage     = 20
  max_allocated_storage = 100
  storage_encrypted     = true

  db_name                     = "minesense"
  username                    = "minesense_admin"
  manage_master_user_password = true

  db_subnet_group_name   = aws_db_subnet_group.postgres.name
  vpc_security_group_ids = [aws_security_group.postgres.id]
  multi_az               = var.postgres_multi_az
  publicly_accessible    = false

  backup_retention_period   = var.backup_retention_days
  deletion_protection       = var.deletion_protection
  skip_final_snapshot       = !var.deletion_protection
  final_snapshot_identifier = var.deletion_protection ? "${var.name}-postgres-final" : null

  tags = var.tags
}

resource "aws_docdb_subnet_group" "documents" {
  name       = "${var.name}-documents"
  subnet_ids = var.private_subnet_ids
  tags       = var.tags
}

resource "aws_security_group" "documents" {
  name        = "${var.name}-documents"
  description = "DocumentDB reachable only from the EKS nodes"
  vpc_id      = var.vpc_id
  tags        = var.tags
}

resource "aws_vpc_security_group_ingress_rule" "documents" {
  for_each = toset(var.allowed_security_group_ids)

  security_group_id            = aws_security_group.documents.id
  referenced_security_group_id = each.value
  from_port                    = 27017
  to_port                      = 27017
  ip_protocol                  = "tcp"
}

resource "random_password" "documents" {
  length  = 32
  special = false
}

resource "aws_secretsmanager_secret" "documents" {
  name                    = "${var.name}/documentdb/master"
  recovery_window_in_days = var.deletion_protection ? 30 : 0
  tags                    = var.tags
}

resource "aws_secretsmanager_secret_version" "documents" {
  secret_id = aws_secretsmanager_secret.documents.id
  secret_string = jsonencode({
    username = "minesense_admin"
    password = random_password.documents.result
    host     = aws_docdb_cluster.documents.endpoint
    port     = 27017
  })
}

resource "aws_docdb_cluster" "documents" {
  cluster_identifier      = "${var.name}-documents"
  engine                  = "docdb"
  master_username         = "minesense_admin"
  master_password         = random_password.documents.result
  db_subnet_group_name    = aws_docdb_subnet_group.documents.name
  vpc_security_group_ids  = [aws_security_group.documents.id]
  storage_encrypted       = true
  backup_retention_period = var.backup_retention_days
  deletion_protection     = var.deletion_protection
  skip_final_snapshot     = !var.deletion_protection

  final_snapshot_identifier = var.deletion_protection ? "${var.name}-documents-final" : null

  tags = var.tags
}

resource "aws_docdb_cluster_instance" "documents" {
  count = var.documentdb_instances

  identifier         = "${var.name}-documents-${count.index}"
  cluster_identifier = aws_docdb_cluster.documents.id
  instance_class     = var.documentdb_instance_class
  tags               = var.tags
}
