# Key Vault Emulator — local dev setup

`docker-compose.yml` runs the [Azure Key Vault Emulator](https://github.com/james-gould/azure-keyvault-emulator)
(`jamesgoulddev/azure-keyvault-emulator`) so `BikeBuilder.API` and `BikeBuilder.Web.Public` can
read their connection strings from a Key Vault-shaped source without a real Azure subscription.
Two one-time steps are needed before it works outside the Testcontainers integration tests
(those manage their own emulator instance + certs automatically via
`AzureKeyVaultEmulator.TestContainers`).

## 1. Generate the TLS certificate

The emulator's own Dockerfile requires exactly one file: a PFX at `/certs/emulator.pfx` with
password `emulator` (its CN doesn't matter — both apps' `Program.cs` bypass cert validation
entirely for the Key Vault connection, since it only ever targets this local emulator). Generate
one with PowerShell (no WSL/OpenSSL needed):

```powershell
New-Item -ItemType Directory -Force -Path keyvault-emulator\certs | Out-Null
$cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(5) -KeyExportPolicy Exportable
Export-PfxCertificate -Cert $cert -FilePath keyvault-emulator\certs\emulator.pfx -Password (ConvertTo-SecureString -String "emulator" -Force -AsPlainText) | Out-Null
Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force
```

The generated PFX is git-ignored — regenerate it on each new dev machine, then
`docker compose up -d` (or `docker compose restart keyvault-emulator` if it's already running).

## 2. Seed the three secrets

Once the container is up (`docker logs bikebuilder-keyvault-emulator-1` should show "Now listening
on: https://[::]:4997"), seed the secrets the apps expect
(`ConnectionStrings--BikeBuilderDb`, `ConnectionStrings--BlobStorage`, `ConnectionStrings--ServiceBus`)
once, directly against the emulator's REST API (no extra tooling needed) — from PowerShell:

```powershell
$token = curl.exe -k -s https://localhost:4997/token
$secrets = @{
    "ConnectionStrings--BikeBuilderDb" = "Server=localhost,1433;Database=BikeBuilderDb;User Id=sa;Password=BikeBuilder!Dev2026;TrustServerCertificate=True"
    "ConnectionStrings--BlobStorage"   = "UseDevelopmentStorage=true"
    "ConnectionStrings--ServiceBus"    = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;"
}
foreach ($name in $secrets.Keys) {
    $body = @{ value = $secrets[$name] } | ConvertTo-Json -Compress
    curl.exe -k -s -X PUT "https://localhost:4997/secrets/$name`?api-version=7.4" -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d $body
}
```

With `Persist: true` set on the `keyvault-emulator` service, secrets survive container restarts,
so this is a true one-time step per dev machine (until the container's data volume is removed).
