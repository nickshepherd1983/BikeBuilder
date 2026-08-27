FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Src/BikeBuilder.Contracts/BikeBuilder.Contracts.csproj Src/BikeBuilder.Contracts/
COPY Src/BikeBuilder.API.Ratings/BikeBuilder.API.Ratings.csproj Src/BikeBuilder.API.Ratings/
RUN dotnet restore Src/BikeBuilder.API.Ratings/BikeBuilder.API.Ratings.csproj
COPY Src/BikeBuilder.Contracts/ Src/BikeBuilder.Contracts/
COPY Src/BikeBuilder.API.Ratings/ Src/BikeBuilder.API.Ratings/
RUN dotnet publish Src/BikeBuilder.API.Ratings/BikeBuilder.API.Ratings.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS final
# files: run the host without a storage account - this app is HTTP-only.
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureWebJobsSecretStorageType=files \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build /app /home/site/wwwroot
EXPOSE 80
