provider "azurerm" {
  features {}
    resource_provider_registrations = "none"
}

data "azurerm_resource_group" "myRG" {
  name = "rg-dersimabbas"
}

resource "azurerm_storage_account" "Compose" {
  name = "docker-compose"
  resource_group_name = data.azurerm_resource_group.myRG.name
  location = data.azurerm_resource_group.myRG.location
  account_tier = "Standard"
  account_replication_type = "GRS"
}

resource "azurerm_storage_container" "container" {
  name = "composeContainer"
  storage_account_id = azurerm_storage_account.Compose.id
  container_access_type = "private"
}