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

module "storage" {
  source = "./Modules/storageAccount"
}

module "webapp" {
  source = "./Modules/WebApp"
}

module "functions" {
  source = "./Modules/AzureFunctions"
}