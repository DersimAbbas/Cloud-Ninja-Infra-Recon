
data "azurerm_resource_group" "RG" {
  name = "RG-dersimabbas"
}

data "azurerm_storage_account" "storage" {
  name = "dersimdockercompose"
  resource_group_name = data.azurerm_resource_group.RG.name
}

resource "azurerm_linux_function_app" "acr_trigger" {
  name = "CloudNinjaBreacher"
  resource_group_name = data.azurerm_resource_group.RG.name
  location = var.location
  service_plan_id = var.service_plan_id
  storage_account_name = data.azurerm_storage_account.storage.name
  storage_account_access_key = data.azurerm_storage_account.storage.primary_access_key
  
  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
  }
    app_settings ={
        "FUNCTIONS_WORKER_RUNTIME" = "dotnet"
        "WEBSITE_RUN_FROM_PACKAGE" = "1"
    }
   
}
output "function_app_url" {
  value = azurerm_linux_function_app.acr_trigger.default_hostname
}

    


  