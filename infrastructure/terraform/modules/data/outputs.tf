output "postgres_endpoint" {
  value = aws_db_instance.postgres.endpoint
}

output "postgres_secret_arn" {
  description = "Secret managed by RDS with the master credentials."
  value       = aws_db_instance.postgres.master_user_secret[0].secret_arn
}

output "documentdb_endpoint" {
  value = aws_docdb_cluster.documents.endpoint
}

output "documentdb_secret_arn" {
  value = aws_secretsmanager_secret.documents.arn
}
