FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BikeBuilder.Contracts/BikeBuilder.Contracts.csproj BikeBuilder.Contracts/
COPY BikeBuilder.API/BikeBuilder.API.csproj BikeBuilder.API/
RUN dotnet restore BikeBuilder.API/BikeBuilder.API.csproj
COPY BikeBuilder.Contracts/ BikeBuilder.Contracts/
COPY BikeBuilder.API/ BikeBuilder.API/
RUN dotnet publish BikeBuilder.API/BikeBuilder.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BikeBuilder.API.dll"]
