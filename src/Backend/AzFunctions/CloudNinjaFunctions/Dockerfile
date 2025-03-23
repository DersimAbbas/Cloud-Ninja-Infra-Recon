FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CloudNinjaFunctions.csproj", "./"]
RUN dotnet restore "CloudNinjaFunctions.csproj"
COPY . .
RUN dotnet build "CloudNinjaFunctions.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CloudNinjaFunctions.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=publish /app/publish /home/site/wwwroot
