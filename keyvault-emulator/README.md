# Key Vault Emulator — local dev setup

`docker-compose.yml` runs the [Azure Key Vault Emulator](https://github.com/james-gould/azure-keyvault-emulator)
(`jamesgoulddev/azure-keyvault-emulator`) so `BikeBuilder.API` and `BikeBuilder.Web.Public` can
read their connection strings from a Key Vault-shaped source without a real Azure subscription.
Two one-time steps are needed before it works outside the Testcontainers integration tests
(those manage their own emulator instance + certs automatically via
`AzureKeyVaultEmulator.TestContainers`).

## 1. Generate the TLS certificate

The emulator requires an HTTPS certificate mounted at `./keyvault-emulator/certs`. Generate and
trust one using the project's own setup script (Windows: run under WSL):

```
wsl -u root bash <(curl -fsSL https://raw.githubusercontent.com/james-gould/azure-keyvault-emulator/refs/heads/master/docs/setup.sh)
```

Point it at `keyvault-emulator/certs` in this repo when prompted for an output directory. The
generated PFX/CRT files are git-ignored — regenerate them on each new dev machine.

## 2. Seed the three secrets

After `docker compose up -d` brings `keyvault-emulator` up, seed the secrets the apps expect
(`ConnectionStrings--BikeBuilderDb`, `ConnectionStrings--BlobStorage`, `ConnectionStrings--ServiceBus`)
once, e.g. from `dotnet fsi`/a scratch console app referencing `Azure.Security.KeyVault.Secrets`:

Reuse the `EmulatorTokenCredential` class from `BikeBuilder.API/Program.cs` (a minimal credential
that fetches a bearer token from the emulator's own `/token` endpoint) - the emulator doesn't
validate it against real Entra ID, so no real Azure login is needed:

```csharp
using Azure.Security.KeyVault.Secrets;

var client = new SecretClient(new Uri("https://localhost:4997"), new EmulatorTokenCredential("https://localhost:4997"));
await client.SetSecretAsync("ConnectionStrings--BikeBuilderDb",
    "Server=localhost,1433;Database=BikeBuilderDb;User Id=sa;Password=BikeBuilder!Dev2026;TrustServerCertificate=True");
await client.SetSecretAsync("ConnectionStrings--BlobStorage", "UseDevelopmentStorage=true");
await client.SetSecretAsync("ConnectionStrings--ServiceBus",
    "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;");
```

With `Persist: true` set on the `keyvault-emulator` service, secrets survive container restarts,
so this is a true one-time step per dev machine (until the container's data volume is removed).
