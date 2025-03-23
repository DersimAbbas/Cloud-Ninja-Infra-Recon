
variable "client_id" {
  description = "Azure Client ID"
  type        = string
  sensitive   = true
}

variable "client_secret" {
  description = "Azure Client Secret"
  type        = string
  sensitive   = true
}

variable "tenant_id" {
  description = "Azure Tenant ID"
  type        = string
  sensitive   = true
}
variable "compose_url" {
  type = string
  description = "Sas URL pointing to docker compose.yml blob"
}

variable "subscription_id" {
  type = string
  description = "sub id for azure"
}