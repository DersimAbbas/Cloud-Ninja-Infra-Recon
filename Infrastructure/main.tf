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
}

module "storageAccount" {
  source = "./Modules/storageAccount"
}

module "webapp" {
  source = "./Modules/WebApp"
  compose_url = var.compose_url
}

module "AzureFunctions" {
  source = "./Modules/AzureFunctions"
  azure_devops_pat = var.azure_devops_pat
}