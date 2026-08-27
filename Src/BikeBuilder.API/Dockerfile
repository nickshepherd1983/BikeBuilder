FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Src/BikeBuilder.Contracts/BikeBuilder.Contracts.csproj Src/BikeBuilder.Contracts/
COPY Src/BikeBuilder.API/BikeBuilder.API.csproj Src/BikeBuilder.API/
RUN dotnet restore Src/BikeBuilder.API/BikeBuilder.API.csproj
COPY Src/BikeBuilder.Contracts/ Src/BikeBuilder.Contracts/
COPY Src/BikeBuilder.API/ Src/BikeBuilder.API/
RUN dotnet publish Src/BikeBuilder.API/BikeBuilder.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BikeBuilder.API.dll"]
