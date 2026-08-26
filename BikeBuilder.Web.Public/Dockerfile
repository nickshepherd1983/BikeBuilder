FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BikeBuilder.Contracts/BikeBuilder.Contracts.csproj BikeBuilder.Contracts/
COPY BikeBuilder.Web.Public/BikeBuilder.Web.Public.csproj BikeBuilder.Web.Public/
RUN dotnet restore BikeBuilder.Web.Public/BikeBuilder.Web.Public.csproj
COPY BikeBuilder.Contracts/ BikeBuilder.Contracts/
COPY BikeBuilder.Web.Public/ BikeBuilder.Web.Public/
# Unlike the other Dockerfiles in this solution, this one deliberately omits --no-restore:
# splitting restore (above) from a --no-restore publish here left the static web assets
# pipeline unable to fingerprint _framework/blazor.web.js, so the rendered <script> tag
# pointed at a route the app couldn't actually serve (observed as a 503/404 depending on
# how the browser requested it) - re-running restore here (fast, since packages are already
# cached from the layer above) as part of a normal publish produces the correct fingerprinted
# route.
RUN dotnet publish BikeBuilder.Web.Public/BikeBuilder.Web.Public.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "BikeBuilder.Web.Public.dll"]
