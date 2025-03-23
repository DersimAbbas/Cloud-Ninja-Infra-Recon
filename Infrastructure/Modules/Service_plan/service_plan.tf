data "azurerm_resource_group" "myRG"{
    name = "RG-dersimabbas"
}

resource "azurerm_service_plan" "linux_plan" {
  name = "linux_service_plan"
  resource_group_name = data.azurerm_resource_group.myRG.name
  location = data.azurerm_resource_group.myRG.location
  os_type = "Linux"
  sku_name = "B1"
}