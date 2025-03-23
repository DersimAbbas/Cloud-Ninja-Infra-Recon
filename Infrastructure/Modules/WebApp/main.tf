
data "azurerm_resource_group" "myRG"{
    name = "RG-dersimabbas"
}

resource "azurerm_linux_web_app" "webapp" {
  name = "Cloud-NinjaRecon"
  resource_group_name = data.azurerm_resource_group.myRG.name
  location = var.location
  service_plan_id = var.service_plan_id

  site_config {
   container_registry_use_managed_identity = false
   application_stack {
     docker_image_name = "compose"
     docker_registry_url = "https://dersimabbas.azurecr.io"
   }
  }
  app_settings = {
       WEBSITES_ENABLE_APP_SERVICE_STORAGE = "false"
       DOCKER_REGISTRY_URL = "https://dersimabbas.azurecr.io"
       DOCKER_COMPOSE_FILE_URL = var.compose_url
  }
  lifecycle {
    ignore_changes = [ 
      app_settings["DOCKER_COMPOSE_FILE_URL"]
     ]
  }
}