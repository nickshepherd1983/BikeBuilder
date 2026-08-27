FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BikeBuilder.API/BikeBuilder.API.csproj BikeBuilder.API/
COPY BikeBuilder.Web/BikeBuilder.Web.csproj BikeBuilder.Web/
RUN dotnet restore BikeBuilder.Web/BikeBuilder.Web.csproj
COPY BikeBuilder.API/ BikeBuilder.API/
COPY BikeBuilder.Web/ BikeBuilder.Web/
RUN dotnet publish BikeBuilder.Web/BikeBuilder.Web.csproj -c Release -o /app --no-restore
ARG API_BASE_ADDRESS=http://127.0.0.1:18100
ARG RATINGS_API_BASE_ADDRESS=http://127.0.0.1:18500
ARG AUTH0_AUTHORITY=https://YOUR_TENANT.auth0.com
ARG AUTH0_CLIENT_ID=YOUR_SPA_CLIENT_ID
ARG AUTH0_AUDIENCE=https://bikebuilder-api
ARG AUTH0_EXTRA_SCOPES=
# Blazor WASM bakes the list of wwwroot/appsettings*.json files to fetch at startup directly into
# the published dotnet.*.js bundle at publish time - a file created only after `dotnet publish`
# runs would never be requested at all, regardless of its content. BikeBuilder.Web/wwwroot ships
# both appsettings.json and appsettings.Development.json in source specifically so publish always
# registers them; this step only overwrites their *content* (and drops the now-stale precompressed
# .gz/.br siblings publish generated for the placeholder content, which nginx would otherwise keep
# serving instead of the plain file - same reasoning applies to both files, not just one).
RUN rm -f /app/wwwroot/appsettings.json.gz /app/wwwroot/appsettings.json.br \
    /app/wwwroot/appsettings.Development.json.gz /app/wwwroot/appsettings.Development.json.br
RUN printf '{"ApiBaseAddress":"%s","RatingsApiBaseAddress":"%s","Auth0":{"Authority":"%s","ClientId":"%s","Audience":"%s","ExtraScopes":"%s"}}' \
      "$API_BASE_ADDRESS" "$RATINGS_API_BASE_ADDRESS" "$AUTH0_AUTHORITY" "$AUTH0_CLIENT_ID" "$AUTH0_AUDIENCE" "$AUTH0_EXTRA_SCOPES" \
      > /app/wwwroot/appsettings.json \
    && cp /app/wwwroot/appsettings.json /app/wwwroot/appsettings.Development.json

FROM nginx:alpine AS final
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY BikeBuilder.Web/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
