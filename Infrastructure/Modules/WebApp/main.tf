
provider "azurerm" {
    features {}
  resource_provider_registrations = "none"
}

data "azurerm_resource_group" "myRG"{
    name = "rg-dersimabbas"
}

resource "azurerm_service_plan" "linux_plan" {
  name = "linux_service_plan"
  resource_group_name = data.azurerm_resource_group.myRG.name
  location = data.azurerm_resource_group.myRG.location
  os_type = "Linux"
  sku_name = "B1"
}

resource "azurerm_linux_web_app" "webapp" {
  name = "Cloud-NinjaRecon"
  resource_group_name = data.azurerm_resource_group.myRG.name
  location = azurerm_service_plan.linux_plan.location
  service_plan_id = azurerm_service_plan.linux_plan.id

  site_config {
   #linux_fx_version = "COMPOSE|${var.compose.url}"
  }
  #app_settings = {
       #WEBSITES_ENABLE_APP_SERVICE_STORAGE = "false"
  #}
}