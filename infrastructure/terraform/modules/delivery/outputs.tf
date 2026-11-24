output "deploy_role_arn" {
  description = "Set as the AWS_DEPLOY_ROLE_ARN variable of the GitHub environment."
  value       = aws_iam_role.deploy.arn
}

output "load_balancer_controller_role_arn" {
  description = "Set as the AWS_LB_CONTROLLER_ROLE_ARN variable of the GitHub environment."
  value       = module.load_balancer_controller_irsa.iam_role_arn
}
