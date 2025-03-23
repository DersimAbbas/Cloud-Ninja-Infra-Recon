terraform {
  required_version = ">=1.3"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>4.0"
    }
  }
}

provider "azurerm" {
  features {}
  resource_provider_registrations = "none"
  subscription_id = var.subscription_id
}

module "storageAccount" {
  source = "./Modules/storageAccount"
}

module "linux_plan" {
  source = "./Modules/Service_plan"
  
}

module "WebApp" {
  source           = "./modules/webapp"
  service_plan_id  = module.linux_plan.id
  location         = module.linux_plan.location
  compose_url      = var.compose_url
}

module "AzureFunctions" {
  source                      = "./modules/Azurefunctions"
  service_plan_id  = module.linux_plan.id
  location         = module.linux_plan.location
}