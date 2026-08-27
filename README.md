# BikeBuilder

> **Heads up: this is prototype-grade code.** This project is my first time using Claude-based
> development, and I'm using it to noodle on different approaches and see what sticks. Treat it
> as a proof of concept / playground rather than a reference implementation — corners are cut,
> patterns shift between features as I experiment, and nothing here is production-hardened.

BikeBuilder is a small microservices playground for building custom bikes: manage a catalog of
components (with images), assemble them into bike builds, rate the builds, and watch activity
land in real time on a public site.

## What's in the solution

| Project | What it is |
| --- | --- |
| `BikeBuilder.Web` | Blazor WebAssembly front end (MudBlazor), Auth0 login, talks gRPC-Web to the API and REST to the Ratings service |
| `BikeBuilder.API` | ASP.NET Core gRPC API (EF Core + SQL Server), component image upload to Azure Blob Storage, publishes events to Service Bus |
| `BikeBuilder.API.Ratings` | Azure Functions (.NET isolated) ratings microservice backed by Cosmos DB, JWT-secured via Auth0 |
| `BikeBuilder.Web.Public` | Blazor Server public site showing live activity toasts (Service Bus → SignalR) |
| `BikeBuilder.Contracts` | Shared event/message contracts |
| `BikeBuilder.DataSeeder` | Console tool that fills the local dev stack with 1000+ real-sounding components, 20 bike builds, and 1–30 ratings each |
| `BikeBuilder.Test.Integration` | End-to-end smoke test: Testcontainers spins up the whole system (SQL Server, Azurite, Service Bus emulator, Key Vault emulator, Cosmos emulator, a stub OIDC issuer, and Docker images of every app) and Playwright drives the real UI, recording video |

## Running it

Local development uses Docker for the backing services:

```powershell
docker compose up -d      # SQL Server, Azurite, Service Bus emulator, Key Vault emulator, Cosmos emulator
```

Then run the apps from Visual Studio or `dotnet run` (the Functions app runs with `func start`).
Auth is a real Auth0 tenant in local dev; integration tests swap in a stub OIDC issuer so they
run fully offline.

To fill the dev stack with realistic sample data (1000+ components, 20 bike builds, ratings):

```powershell
dotnet run --project Src/BikeBuilder.DataSeeder             # refuses if components already exist
dotnet run --project Src/BikeBuilder.DataSeeder -- --reset  # wipes components/builds/ratings first
```

## Tests

```powershell
dotnet test Src/BikeBuilder.Test.Integration
```

One end-to-end test covers the whole journey: log in, create a component, upload an image,
build a bike, rate it, and verify the live toasts on the public site. Requires Docker.
Debugging the test from Visual Studio's Test Explorer runs the browser headed; videos land in
`TestResults/videos`.
