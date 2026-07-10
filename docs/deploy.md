# Deploy

## Azure Function APP

### 1. create storage account
az storage account create --name myfuncdemo --resource-group myfuncdemo --location eastasia --sku Standard_LRS

![picture 6](../images/0686e379a5a15c55ed02cf2c50d8f4bdefd42d22cae78a32abb1812e6fe5a3f9.png)  

### 2. create Function App
az functionapp create --resource-group myfuncdemo --consumption-plan-location eastasia --runtime dotnet-isolated --runtime-version 8.0 --functions-version 4 --name MyFuncDemoApp --storage-account myfuncdemo

![picture 4](../images/d2752f951a92c75049b4aa78f0424802e890bccf54d65b575df8759c0b808609.png)  

### 3.deploy func
cd src/FunctionApp
func azure functionapp publish MyFuncDemoApp --dotnet-isolated

![picture 5](../images/1cdb2a8de5bf48b3a9b35c0e3ad25c492266a4b00bb2c39d3d6521d37f41a53d.png)  


## Container

![picture 7](../images/e87a729bd58f264d77f65f08fe9a155a29d22b928ae54052a9b9d99d46c98398.png)  
