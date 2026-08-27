FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BikeBuilder.Contracts/BikeBuilder.Contracts.csproj BikeBuilder.Contracts/
COPY BikeBuilder.API.Ratings/BikeBuilder.API.Ratings.csproj BikeBuilder.API.Ratings/
RUN dotnet restore BikeBuilder.API.Ratings/BikeBuilder.API.Ratings.csproj
COPY BikeBuilder.Contracts/ BikeBuilder.Contracts/
COPY BikeBuilder.API.Ratings/ BikeBuilder.API.Ratings/
RUN dotnet publish BikeBuilder.API.Ratings/BikeBuilder.API.Ratings.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS final
# files: run the host without a storage account - this app is HTTP-only.
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureWebJobsSecretStorageType=files \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build /app /home/site/wwwroot
EXPOSE 80
