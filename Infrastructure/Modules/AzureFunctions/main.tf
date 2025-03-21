provider "azurerm" {
  features {}
  resource_provider_registrations = "none"
}

data "azurerm_resource_group" "RG" {
  name = "rg-dersimabbas"
}

data "azurerm_service_plan" "existing"{
    name = "linux_service_plan"
    resource_group_name = data.azurerm_resource_group.RG.name
} 
data "azurerm_storage_account" "storage" {
  name = "dersimdockercompose"
  resource_group_name = data.azurerm_resource_group.RG.name
}

resource "azurerm_linux_function_app" "acr_trigger" {
  name = "acr-pipeline-trigger"
  resource_group_name = data.azurerm_resource_group.RG.name
  location = data.azurerm_resource_group.RG.location
  service_plan_id = data.azurerm_service_plan.existing.id
  storage_account_name = data.azurerm_storage_account.storage.name
  storage_account_access_key = data.azurerm_storage_account.storage.primary_access_key
  
  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
  }
    app_settings ={
        "FUNCTIONS_WORKER_RUNTIME" = "dotnet"
        "AZURE_DEVOPS_PAT" = var.azure_devops_pat
        "WEBSITE_RUN_FROM_PACKAGE" = "1"
    }
    identity {
      type = "SystemAssigned"
    }
}
output "function_app_url" {
  value = azurerm_linux_function_app.acr_trigger.default_hostname
}
output "function_app_principal_id" {
  value = azurerm_linux_function_app.acr_trigger.identity[0].principal_id
}

    


  