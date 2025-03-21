provider "azurerm" {
  features {}
    resource_provider_registrations = "none"
}

data "azurerm_resource_group" "myRG" {
  name = "rg-dersimabbas"
}

data "azurerm_storage_account" "storage" {
    name = "dersimdockercompose"
    resource_group_name = data.azurerm_resource_group.myRG.name
}

data "azurerm_storage_container" "container" {
  name = "compose"
  storage_account_name = data.azurerm_storage_account.storage.name
}

/*data "azurerm_storage_blob" "compose_file" {
  name = "docker-compose.yml"
  storage_account_name = data.azurerm_storage_account.storage.name
  storage_container_name = data.azurerm_storage_container.container.name
}*/

data "azurerm_storage_account_sas" "account_sas" {
  connection_string = data.azurerm_storage_account.storage.primary_connection_string
  https_only = false
  
  resource_types {
    service = true
    container = true
    object = false
  }
  services {
    blob = true
    queue = false
    table = false
    file = true
  }

  start = "2025-03-21T18:20:00Z"
  expiry = "2025-03-27T18:20:00Z"

  permissions {
    read = true
    write = true
    delete = false
    list = false
    add = true
    create = true
    update = true
    process = false
    tag = false
    filter = false
  }
}
/*output "sas_url_string" {
  value = data.azurerm_storage_account_sas.account_sas
}*/

