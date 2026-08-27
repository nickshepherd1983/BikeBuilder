using System.Diagnostics;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.TestContainers;
using BikeBuilder.API.Data;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using Microsoft.EntityFrameworkCore;
using Testcontainers.Azurite;
using Testcontainers.MsSql;

namespace BikeBuilder.Test.Integration;

public sealed class BikeBuilderAppFixture : IAsyncLifetime
{
  public const int ApiHostPort = 18100;
  public const int WebHostPort = 18200;
  public const int WebPublicHostPort = 18300;
  public const int OidcHostPort = 18400;
  public const int RatingsHostPort = 18500;

  public const string OidcTestUsername = "testuser";
  public const string OidcTestPassword = "password";

  // 127.0.0.1 rather than "localhost" - on this Windows/Docker Desktop setup, the .NET
  // HttpClient used for the readiness check below and the Chromium browser Playwright
  // launches were observed resolving "localhost" differently, with only one of the two
  // reliably reaching the published container ports.
  public string ApiBaseAddress => $"http://127.0.0.1:{ApiHostPort}";
  public string WebBaseAddress => $"http://127.0.0.1:{WebHostPort}";
  public string WebPublicBaseAddress => $"http://127.0.0.1:{WebPublicHostPort}";
  public string RatingsBaseAddress => $"http://127.0.0.1:{RatingsHostPort}";
  public IBrowser Browser { get; private set; } = null!;

  public static readonly string VideosDir = Path.Combine(AppContext.BaseDirectory, "TestResults", "videos");

  /// <summary>
  /// Creates a page in a context that records video to TestResults/videos. Playwright only
  /// finalizes the video when the context closes, so callers must dispose the page via
  /// <see cref="SaveVideoAsync"/> (which closes the context) rather than page.CloseAsync().
  /// </summary>
  public async Task<IPage> CreatePageAsync()
  {
    var context = await Browser.NewContextAsync(new()
    {
      RecordVideoDir = VideosDir,
      RecordVideoSize = new() { Width = 1280, Height = 720 }
    });
    return await context.NewPageAsync();
  }

  /// <summary>Closes the page's context (finalizing the recording) and renames the video.</summary>
  public static async Task SaveVideoAsync(IPage page, string name)
  {
    await page.Context.CloseAsync();
    if (page.Video is not null)
    {
      await page.Video.SaveAsAsync(Path.Combine(VideosDir, $"{name}.webm"));
      await page.Video.DeleteAsync();
    }
  }

  const string ServiceBusSqlPassword = "BikeBuilder!Bus2026";
  const string ServiceBusConnectionString =
      "Endpoint=sb://servicebus-emulator;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

  const int KeyVaultContainerPort = 4997;
  const string KeyVaultNetworkAlias = "keyvault-emulator";
  static readonly string KeyVaultVaultUri = $"https://{KeyVaultNetworkAlias}:{KeyVaultContainerPort}";

  // The issuer the browser uses must equal the token's iss claim. IdentityServer pins the
  // issuer via IssuerUri but generates the discovery document's *endpoint* URLs from each
  // request's host, so the browser (via the host port binding) and the API (via the
  // "oidc-mock" network alias) can both reach the stub while agreeing on this one issuer.
  static readonly string OidcIssuerUri = $"http://127.0.0.1:{OidcHostPort}";
  const string OidcNetworkAlias = "oidc-mock";
  const string OidcAudience = "bikebuilder-api";
  const string OidcClientId = "bikebuilder-web";

  INetwork _network = null!;
  MsSqlContainer _sql = null!;
  AzuriteContainer _azurite = null!;
  IContainer _serviceBusSql = null!;
  IContainer _serviceBus = null!;
  AzureKeyVaultEmulatorContainer _keyVault = null!;
  IContainer _oidcMock = null!;
  IContainer _cosmos = null!;
  IFutureDockerImage _ratingsImage = null!;
  IContainer _ratings = null!;
  IFutureDockerImage _apiImage = null!;
  IFutureDockerImage _webImage = null!;
  IFutureDockerImage _webPublicImage = null!;
  IContainer _api = null!;
  IContainer _web = null!;
  IContainer _webPublic = null!;
  IPlaywright _playwright = null!;

  public async Task InitializeAsync()
  {
    var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
    if (exitCode != 0)
    {
      throw new InvalidOperationException($"playwright install failed with exit code {exitCode}");
    }

    var solutionDir = CommonDirectoryPath.GetSolutionDirectory().DirectoryPath;

    _network = new NetworkBuilder().Build();
    await _network.CreateAsync();

    // The parameterless MsSqlBuilder/ContainerBuilder constructors are marked obsolete in
    // Testcontainers 4.14.0 in favor of overloads that pin an explicit image, but still
    // resolve their documented default images correctly today.
#pragma warning disable CS0618
    _sql = new MsSqlBuilder()
        .WithDatabase("BikeBuilderDb")
        .WithPassword("BikeBuilder!Test2026")
        .WithNetwork(_network)
        .WithNetworkAliases("sql")
        .Build();
#pragma warning restore CS0618

    // Testcontainers.Azurite pins 3.28.0 by default, which predates Azurite's
    // AZURITE_SKIP_API_VERSION_CHECK env var (added in 3.37.0) - needed because the
    // installed Azure.Storage.Blobs client sends a newer x-ms-version than 3.28.0 recognizes.
    // (The --skipApiVersionCheck command-line equivalent can't be used here: overriding
    // AzuriteBuilder's command breaks its container networking - see
    // https://github.com/Azure/Azurite/issues/2432.)
    _azurite = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.37.0")
        .WithNetwork(_network)
        .WithNetworkAliases("azurite")
        .WithEnvironment("AZURITE_SKIP_API_VERSION_CHECK", "true")
        .Build();

    // Mirrors docker-compose.yml's servicebus-sql/servicebus-emulator services: the Service
    // Bus emulator requires its own paired Azure SQL Edge instance (separate from the app's
    // own "sql" container/password) and waits internally for it to become reachable, so no
    // explicit wait-for-SQL-readiness step is needed here beyond just starting it.
    _serviceBusSql = new ContainerBuilder()
        .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
        .WithNetwork(_network)
        .WithNetworkAliases("servicebus-sql")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_SA_PASSWORD", ServiceBusSqlPassword)
        .Build();

    _serviceBus = new ContainerBuilder()
        .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
        .WithNetwork(_network)
        .WithNetworkAliases("servicebus-emulator")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("SQL_SERVER", "servicebus-sql")
        .WithEnvironment("MSSQL_SA_PASSWORD", ServiceBusSqlPassword)
        .WithBindMount(Path.Combine(solutionDir, "Tools", "servicebus-emulator", "Config.json"), "/ServiceBus_Emulator/ConfigFiles/Config.json")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Emulator Service is Successfully Up!"))
        .Build();

    _keyVault = new AzureKeyVaultEmulatorContainer();

    // Stub OIDC issuer standing in for Auth0. 0.8.6 (Duende IdentityServer 6.3 on .NET 6)
    // predates the image's .NET 8 rebase, so the container listens on port 80; its quickstart
    // login form uses "Input.Username"/"Input.Password" - see NavigationHelper's login handling.
    _oidcMock = new ContainerBuilder("ghcr.io/soluto/oidc-server-mock:0.8.6")
        .WithNetwork(_network)
        .WithNetworkAliases(OidcNetworkAlias)
        .WithPortBinding(OidcHostPort, 80)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        // CookieSameSiteMode=Lax: the default of SameSite=None requires Secure, and Chromium
        // silently drops such cookies over plain http - the login POST would succeed but the
        // browser would return to /connect/authorize with no session, looping back to the
        // login form forever.
        .WithEnvironment("SERVER_OPTIONS_INLINE",
            $$$"""{"IssuerUri":"{{{OidcIssuerUri}}}","AccessTokenJwtType":"JWT","Authentication":{"CookieSameSiteMode":"Lax"}}""")
        .WithEnvironment("API_SCOPES_INLINE",
            $$"""[{"Name":"{{OidcAudience}}"}]""")
        // UserClaims: puts the user's name claim into access tokens for this API, the way a
        // real Auth0 tenant would via an Action - the ratings service reads it for userName.
        .WithEnvironment("API_RESOURCES_INLINE",
            $$"""[{"Name":"{{OidcAudience}}","Scopes":["{{OidcAudience}}"],"UserClaims":["name"]}]""")
        .WithEnvironment("CLIENTS_CONFIGURATION_INLINE",
            $$"""
            [{
              "ClientId": "{{OidcClientId}}",
              "AllowedGrantTypes": ["authorization_code"],
              "RequirePkce": true,
              "RequireClientSecret": false,
              "RedirectUris": ["{{WebBaseAddress}}/authentication/login-callback"],
              "PostLogoutRedirectUris": ["{{WebBaseAddress}}/authentication/logout-callback"],
              "AllowedCorsOrigins": ["{{WebBaseAddress}}"],
              "AllowedScopes": ["openid", "profile", "{{OidcAudience}}"],
              "AccessTokenType": "Jwt",
              "AllowAccessTokensViaBrowser": true
            }]
            """)
        .WithEnvironment("USERS_CONFIGURATION_INLINE",
            $$"""
            [{
              "SubjectId": "test-user",
              "Username": "{{OidcTestUsername}}",
              "Password": "{{OidcTestPassword}}",
              "Claims": [{"Type": "name", "Value": "Test User", "ValueType": "string"}]
            }]
            """)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(r => r.ForPort(80).ForPath("/.well-known/openid-configuration")))
        .Build();

    // The vnext Cosmos emulator's default http mode is unsupported by the .NET SDK, so it
    // runs https with a self-signed cert the ratings app is configured to trust. The app
    // reaches it by network alias only; the bound probe port is just for the wait strategy.
    _cosmos = new ContainerBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-latest")
        .WithNetwork(_network)
        .WithNetworkAliases("cosmos-emulator")
        .WithEnvironment("PROTOCOL", "https")
        .WithEnvironment("ENABLE_EXPLORER", "false")
        .WithPortBinding(8080, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("/ready")))
        .Build();

    await Task.WhenAll(_sql.StartAsync(), _azurite.StartAsync(), _serviceBusSql.StartAsync(), _keyVault.StartAsync(), _oidcMock.StartAsync(), _cosmos.StartAsync());
    await _serviceBus.StartAsync();

    // AzureKeyVaultEmulatorContainer doesn't expose Testcontainers' network-attachment API
    // (WithNetwork/WithNetworkAliases) - it's a standalone wrapper, not a DotNet.Testcontainers
    // IContainer. Join it to _network after the fact via the Docker CLI using its container Id,
    // so _api/_webPublic (already on _network) can reach it at KeyVaultVaultUri.
    await RunDockerCommandAsync($"network connect {_network.Name} {_keyVault.Id} --alias {KeyVaultNetworkAlias}");

    // Seed the secrets the API/Web.Public containers will fetch for themselves at startup,
    // using the emulator's host-mapped port (this runs on the test host, not inside a container).
    // The wrapper's AzureKeyVaultEmulatorClientHelper returns a client pinned to the port cached
    // at StartAsync, which CI diagnostics showed going stale (client on :32769 while docker port
    // reported :32778) - so build our own client against the port Docker reports right now.
    var keyVaultSecretClient = CreateKeyVaultSeedClient(await GetKeyVaultHostPortAsync());
    Task SeedFirstSecretAsync() => keyVaultSecretClient.SetSecretAsync("ConnectionStrings--BikeBuilderDb",
        "Server=sql,1433;Database=BikeBuilderDb;User Id=sa;Password=BikeBuilder!Test2026;TrustServerCertificate=True");

    // On Linux CI runners the network connect above has been seen to leave the emulator's
    // host-mapped port dead (connection refused) even though the emulator is listening inside
    // the container. If a short retry window doesn't recover, restart the container so Docker
    // rebuilds the host port-forwarding with both networks attached, then re-resolve the
    // client since the mapped port moves on restart.
    try
    {
      await WaitUntilSucceedsAsync(SeedFirstSecretAsync, timeoutSeconds: 30);
    }
    catch (InvalidOperationException)
    {
      await RunDockerCommandAsync($"restart {_keyVault.Id}");
      keyVaultSecretClient = CreateKeyVaultSeedClient(await GetKeyVaultHostPortAsync());
      try
      {
        await WaitUntilSucceedsAsync(SeedFirstSecretAsync, timeoutSeconds: 150);
      }
      catch (InvalidOperationException ex)
      {
        var diagnostics = await CaptureDockerDiagnosticsAsync(_keyVault.Id);
        throw new InvalidOperationException(
            $"Key Vault emulator never became reachable (client target: {keyVaultSecretClient.VaultUri}).\n{diagnostics}", ex);
      }
    }
    await keyVaultSecretClient.SetSecretAsync("ConnectionStrings--BlobStorage",
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;");
    await keyVaultSecretClient.SetSecretAsync("ConnectionStrings--ServiceBus", ServiceBusConnectionString);
    // Well-known Cosmos emulator account key (public, not a secret in the real sense).
    await keyVaultSecretClient.SetSecretAsync("ConnectionStrings--Cosmos",
        "AccountEndpoint=https://cosmos-emulator:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");

    var options = new DbContextOptionsBuilder<BikeBuilderDbContext>()
        .UseSqlServer(_sql.GetConnectionString())
        .Options;
    await using (var db = new BikeBuilderDbContext(options))
    {
      await db.Database.MigrateAsync();
    }

    _apiImage = new ImageFromDockerfileBuilder()
        .WithContextDirectory(solutionDir)
        .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "Src/BikeBuilder.API")
        .WithDockerfile("Dockerfile")
        .WithName("bikebuilder-api:test")
        .Build();

    _webImage = new ImageFromDockerfileBuilder()
        .WithContextDirectory(solutionDir)
        .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "Src/BikeBuilder.Web")
        .WithDockerfile("Dockerfile")
        .WithBuildArgument("API_BASE_ADDRESS", ApiBaseAddress)
        .WithBuildArgument("AUTH0_AUTHORITY", OidcIssuerUri)
        .WithBuildArgument("AUTH0_CLIENT_ID", OidcClientId)
        .WithBuildArgument("AUTH0_AUDIENCE", OidcAudience)
        // The stub mints the aud claim from requested API scopes, not Auth0's audience param.
        .WithBuildArgument("AUTH0_EXTRA_SCOPES", OidcAudience)
        .WithBuildArgument("RATINGS_API_BASE_ADDRESS", RatingsBaseAddress)
        .WithName("bikebuilder-web:test")
        .Build();

    _webPublicImage = new ImageFromDockerfileBuilder()
        .WithContextDirectory(solutionDir)
        .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "Src/BikeBuilder.Web.Public")
        .WithDockerfile("Dockerfile")
        .WithName("bikebuilder-web-public:test")
        .Build();

    _ratingsImage = new ImageFromDockerfileBuilder()
        .WithContextDirectory(solutionDir)
        .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "Src/BikeBuilder.API.Ratings")
        .WithDockerfile("Dockerfile")
        .WithName("bikebuilder-ratings:test")
        .Build();

    await Task.WhenAll(_apiImage.CreateAsync(), _webImage.CreateAsync(), _webPublicImage.CreateAsync(), _ratingsImage.CreateAsync());

#pragma warning disable CS0618
    _api = new ContainerBuilder()
        .WithImage(_apiImage)
        .WithNetwork(_network)
        .WithNetworkAliases("api")
        .WithPortBinding(ApiHostPort, 8080)
        .WithEnvironment("KeyVault__VaultUri", KeyVaultVaultUri)
        .WithEnvironment("WebAppOrigins__0", WebBaseAddress)
        // The API fetches discovery/JWKS in-network via the alias; the discovery document
        // still reports OidcIssuerUri as issuer, which is what token iss claims carry.
        .WithEnvironment("Auth0__Authority", $"http://{OidcNetworkAlias}")
        .WithEnvironment("Auth0__Audience", OidcAudience)
        .WithEnvironment("Auth0__RequireHttpsMetadata", "false")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("/")))
        .Build();

    // The anonymous list endpoint returns 200 [] for any id, so probing it verifies the whole
    // startup chain: Functions host + worker up, KV secrets fetched, Cosmos provisioned. The
    // host's own "/" homepage responds 200 long before the worker is ready - don't probe it.
    _ratings = new ContainerBuilder()
        .WithImage(_ratingsImage)
        .WithNetwork(_network)
        .WithNetworkAliases("ratings")
        .WithPortBinding(RatingsHostPort, 80)
        .WithEnvironment("KeyVault__VaultUri", KeyVaultVaultUri)
        .WithEnvironment("Auth0__Authority", $"http://{OidcNetworkAlias}")
        .WithEnvironment("Auth0__Audience", OidcAudience)
        .WithEnvironment("Auth0__RequireHttpsMetadata", "false")
        .WithEnvironment("WebAppOrigins__0", WebBaseAddress)
        .WithEnvironment("Cosmos__DisableServerCertificateValidation", "true")
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(
                r => r.ForPort(80).ForPath("/api/bikebuilds/warmup/ratings"),
                w => w.WithTimeout(TimeSpan.FromMinutes(3))))
        .Build();

    await Task.WhenAll(_api.StartAsync(), _ratings.StartAsync());

    _web = new ContainerBuilder()
        .WithImage(_webImage)
        .WithPortBinding(WebHostPort, 80)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(80).ForPath("/")))
        .Build();

    // Unlike _web (a WASM app the browser calls directly), _webPublic's own
    // ServiceBusListenerBackgroundService needs server-side reachability to the Service
    // Bus emulator, so it must join _network.
    _webPublic = new ContainerBuilder()
        .WithImage(_webPublicImage)
        .WithNetwork(_network)
        .WithNetworkAliases("web-public")
        .WithPortBinding(WebPublicHostPort, 8080)
        .WithEnvironment("KeyVault__VaultUri", KeyVaultVaultUri)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("/")))
        .Build();
#pragma warning restore CS0618
    await Task.WhenAll(_web.StartAsync(), _webPublic.StartAsync());

    // Testcontainers' own readiness probes above already confirmed both containers respond
    // on their published ports, but on Windows/Docker Desktop the host-side port-forwarding
    // for a freshly published port can take a moment longer to fully propagate than that -
    // re-check the exact host+port a browser will use (not just what Testcontainers probed)
    // before handing control to Playwright, so the app's first real page load doesn't race it.
    await WaitUntilReachableAsync(ApiBaseAddress);
    await WaitUntilReachableAsync(WebBaseAddress);
    await WaitUntilReachableAsync(WebPublicBaseAddress);
    await WaitUntilReachableAsync(OidcIssuerUri);
    await WaitUntilReachableAsync($"{RatingsBaseAddress}/api/bikebuilds/warmup/ratings");

    _playwright = await Playwright.CreateAsync();
    // Set HEADED=1 to watch the browser while the test runs (e.g. `$env:HEADED=1` in
    // PowerShell before `dotnet test`, or via .runsettings). Debugging the test (Visual
    // Studio Test Explorer "Debug") attaches a debugger, so that runs headed too.
    var headed = Environment.GetEnvironmentVariable("HEADED") == "1" || Debugger.IsAttached;
    Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    {
      Headless = !headed,
      SlowMo = headed ? 250 : 0,
      // Chrome's speculative background-networking features (preconnect, DNS/network
      // prediction, connection warm-up heuristics) have been observed interacting badly
      // with this environment's Docker-forwarded loopback ports, surfacing as intermittent
      // ERR_CONNECTION_REFUSED on the app's own gRPC-Web calls despite the same origin
      // being reachable via a plain fetch moments before or after - disable that class of
      // feature outright rather than chase it further.
      Args =
        [
            "--disable-background-networking",
                "--disable-features=NetworkPrediction,PreconnectToSearch",
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-renderer-backgrounding",
                "--disable-ipc-flooding-protection",
                "--no-first-run",
            ],
    });
  }

  // Resolves the emulator's CURRENT host-mapped port straight from Docker: the wrapper's own
  // helper (and Testcontainers' mapped-port API) reflect the state cached at StartAsync, which
  // goes stale if the container is restarted.
  async Task<int> GetKeyVaultHostPortAsync()
  {
    using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("docker", $"port {_keyVault.Id} {KeyVaultContainerPort}/tcp")
    {
      RedirectStandardError = true,
      RedirectStandardOutput = true,
      UseShellExecute = false,
    })!;
    var output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();

    var firstBinding = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
        ?? throw new InvalidOperationException($"docker port reported no binding for the Key Vault emulator ({_keyVault.Id}).");
    return int.Parse(firstBinding[(firstBinding.LastIndexOf(':') + 1)..]);
  }

  // Same emulator-friendly client the apps use (see BikeBuilder.API/Program.cs): the emulator's
  // self-signed cert isn't in the host trust store, and its /token endpoint stands in for AAD.
  static SecretClient CreateKeyVaultSeedClient(int hostPort)
  {
    var vaultUri = $"https://localhost:{hostPort}";
    return new SecretClient(new Uri(vaultUri), new EmulatorTokenCredential(vaultUri), new SecretClientOptions
    {
      DisableChallengeResourceVerification = true,
      Transport = new HttpClientTransport(new HttpClientHandler
      {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
      })
    });
  }

  // Best-effort container state + logs for failure messages; never throws.
  static async Task<string> CaptureDockerDiagnosticsAsync(string containerId)
  {
    try
    {
      using var state = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("docker", $"inspect --format {{{{.State.Status}}}}/{{{{.State.ExitCode}}}} {containerId}")
      {
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        UseShellExecute = false,
      })!;
      var stateText = (await state.StandardOutput.ReadToEndAsync()).Trim();
      await state.WaitForExitAsync();

      using var logs = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("docker", $"logs --tail 100 {containerId}")
      {
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        UseShellExecute = false,
      })!;
      var stdout = await logs.StandardOutput.ReadToEndAsync();
      var stderr = await logs.StandardError.ReadToEndAsync();
      await logs.WaitForExitAsync();

      using var ports = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("docker", $"port {containerId}")
      {
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        UseShellExecute = false,
      })!;
      var portsText = (await ports.StandardOutput.ReadToEndAsync()).Trim();
      await ports.WaitForExitAsync();

      return $"Container state: {stateText}\nPublished ports:\n{portsText}\nContainer logs:\n{stdout}\n{stderr}";
    }
    catch (Exception ex)
    {
      return $"(failed to capture container diagnostics: {ex.Message})";
    }
  }

  static async Task RunDockerCommandAsync(string arguments)
  {
    using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("docker", arguments)
    {
      RedirectStandardError = true,
      RedirectStandardOutput = true,
      UseShellExecute = false,
    })!;
    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
      var error = await process.StandardError.ReadToEndAsync();
      throw new InvalidOperationException($"docker {arguments} failed: {error}");
    }
  }

  // For host-side calls against a freshly published container port: the container's own
  // readiness probe can pass before the host-mapped port-forwarding accepts connections,
  // especially on slow CI runners.
  static async Task WaitUntilSucceedsAsync(Func<Task> action, int timeoutSeconds = 90)
  {
    var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
    while (true)
    {
      try
      {
        await action();
        return;
      }
      catch (Exception ex)
      {
        if (DateTime.UtcNow >= deadline)
          throw new InvalidOperationException($"Action did not succeed within {timeoutSeconds}s.", ex);

        await Task.Delay(TimeSpan.FromSeconds(2));
      }
    }
  }

  static async Task WaitUntilReachableAsync(string baseUrl)
  {
    using var client = new HttpClient();
    var deadline = DateTime.UtcNow.AddSeconds(30);
    Exception? lastError = null;

    while (DateTime.UtcNow < deadline)
    {
      try
      {
        using var response = await client.GetAsync(baseUrl);
        return;
      }
      catch (Exception ex)
      {
        lastError = ex;
        await Task.Delay(TimeSpan.FromMilliseconds(500));
      }
    }

    throw new InvalidOperationException($"{baseUrl} did not become reachable in time.", lastError);
  }

  public async Task DisposeAsync()
  {
    // InitializeAsync may have thrown partway through, leaving later fields unset - guard
    // each teardown step so a partial-startup failure doesn't also mask a NullReferenceException.
    if (Browser is not null)
      await Browser.CloseAsync();

    _playwright?.Dispose();

    if (_web is not null)
      await _web.DisposeAsync();

    if (_webPublic is not null)
      await _webPublic.DisposeAsync();

    if (_api is not null)
      await _api.DisposeAsync();

    if (_ratings is not null)
      await _ratings.DisposeAsync();

    if (_cosmos is not null)
      await _cosmos.DisposeAsync();

    if (_azurite is not null)
      await _azurite.DisposeAsync();

    if (_serviceBus is not null)
      await _serviceBus.DisposeAsync();

    if (_serviceBusSql is not null)
      await _serviceBusSql.DisposeAsync();

    if (_keyVault is not null)
      await _keyVault.DisposeAsync();

    if (_oidcMock is not null)
      await _oidcMock.DisposeAsync();

    if (_sql is not null)
      await _sql.DisposeAsync();

    if (_apiImage is not null)
      await _apiImage.DisposeAsync();

    if (_webImage is not null)
      await _webImage.DisposeAsync();

    if (_webPublicImage is not null)
      await _webPublicImage.DisposeAsync();

    if (_ratingsImage is not null)
      await _ratingsImage.DisposeAsync();

    if (_network is not null)
      await _network.DeleteAsync();
  }
}

// Mirrors AzureKeyVaultEmulator.Client's own (now-obsolete) EmulatedTokenCredential - fetches a
// bearer token from the emulator's /token endpoint - but with a cert-trusting HttpClient, since
// that type's internal HttpClient can't be configured and fails TLS against untrusted certs.
// Same copy as in BikeBuilder.API/Program.cs.
sealed class EmulatorTokenCredential(string vaultUri) : TokenCredential
{
  static readonly HttpClient Client = new(new HttpClientHandler
  {
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
  });

  public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
      GetTokenAsync(requestContext, cancellationToken).AsTask().GetAwaiter().GetResult();

  public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
  {
    var response = await Client.GetAsync($"{vaultUri}/token", cancellationToken);
    response.EnsureSuccessStatusCode();
    var token = await response.Content.ReadAsStringAsync(cancellationToken);
    return new AccessToken(token, DateTimeOffset.UtcNow.AddDays(1));
  }
}
