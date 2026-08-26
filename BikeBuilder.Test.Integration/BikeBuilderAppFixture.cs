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

    // 127.0.0.1 rather than "localhost" - on this Windows/Docker Desktop setup, the .NET
    // HttpClient used for the readiness check below and the Chromium browser Playwright
    // launches were observed resolving "localhost" differently, with only one of the two
    // reliably reaching the published container ports.
    public string ApiBaseAddress => $"http://127.0.0.1:{ApiHostPort}";
    public string WebBaseAddress => $"http://127.0.0.1:{WebHostPort}";
    public string WebPublicBaseAddress => $"http://127.0.0.1:{WebPublicHostPort}";
    public IBrowser Browser { get; private set; } = null!;

    private const string ServiceBusSqlPassword = "BikeBuilder!Bus2026";
    private const string ServiceBusConnectionString =
        "Endpoint=sb://servicebus-emulator;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    private INetwork _network = null!;
    private MsSqlContainer _sql = null!;
    private AzuriteContainer _azurite = null!;
    private IContainer _serviceBusSql = null!;
    private IContainer _serviceBus = null!;
    private IFutureDockerImage _apiImage = null!;
    private IFutureDockerImage _webImage = null!;
    private IFutureDockerImage _webPublicImage = null!;
    private IContainer _api = null!;
    private IContainer _web = null!;
    private IContainer _webPublic = null!;
    private IPlaywright _playwright = null!;

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
            .WithBindMount(Path.Combine(solutionDir, "servicebus-emulator", "Config.json"), "/ServiceBus_Emulator/ConfigFiles/Config.json")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Emulator Service is Successfully Up!"))
            .Build();

        await Task.WhenAll(_sql.StartAsync(), _azurite.StartAsync(), _serviceBusSql.StartAsync());
        await _serviceBus.StartAsync();

        var options = new DbContextOptionsBuilder<BikeBuilderDbContext>()
            .UseSqlServer(_sql.GetConnectionString())
            .Options;
        await using (var db = new BikeBuilderDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        _apiImage = new ImageFromDockerfileBuilder()
            .WithContextDirectory(solutionDir)
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "BikeBuilder.API")
            .WithDockerfile("Dockerfile")
            .WithName("bikebuilder-api:test")
            .Build();

        _webImage = new ImageFromDockerfileBuilder()
            .WithContextDirectory(solutionDir)
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "BikeBuilder.Web")
            .WithDockerfile("Dockerfile")
            .WithBuildArgument("API_BASE_ADDRESS", ApiBaseAddress)
            .WithName("bikebuilder-web:test")
            .Build();

        _webPublicImage = new ImageFromDockerfileBuilder()
            .WithContextDirectory(solutionDir)
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "BikeBuilder.Web.Public")
            .WithDockerfile("Dockerfile")
            .WithName("bikebuilder-web-public:test")
            .Build();

        await Task.WhenAll(_apiImage.CreateAsync(), _webImage.CreateAsync(), _webPublicImage.CreateAsync());

#pragma warning disable CS0618
        _api = new ContainerBuilder()
            .WithImage(_apiImage)
            .WithNetwork(_network)
            .WithNetworkAliases("api")
            .WithPortBinding(ApiHostPort, 8080)
            .WithEnvironment("ConnectionStrings__BikeBuilderDb",
                "Server=sql,1433;Database=BikeBuilderDb;User Id=sa;Password=BikeBuilder!Test2026;TrustServerCertificate=True")
            .WithEnvironment("ConnectionStrings__BlobStorage",
                "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;")
            .WithEnvironment("ConnectionStrings__ServiceBus", ServiceBusConnectionString)
            .WithEnvironment("WebAppOrigins__0", WebBaseAddress)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080).ForPath("/")))
            .Build();
        await _api.StartAsync();

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
            .WithEnvironment("ConnectionStrings__ServiceBus", ServiceBusConnectionString)
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

        _playwright = await Playwright.CreateAsync();
        // Set HEADED=1 to watch the browser while the test runs (e.g. `$env:HEADED=1` in
        // PowerShell before `dotnet test`); defaults to headless otherwise.
        var headed = Environment.GetEnvironmentVariable("HEADED") == "1";
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

    private static async Task WaitUntilReachableAsync(string baseUrl)
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
        {
            await Browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_web is not null)
        {
            await _web.DisposeAsync();
        }

        if (_webPublic is not null)
        {
            await _webPublic.DisposeAsync();
        }

        if (_api is not null)
        {
            await _api.DisposeAsync();
        }

        if (_azurite is not null)
        {
            await _azurite.DisposeAsync();
        }

        if (_serviceBus is not null)
        {
            await _serviceBus.DisposeAsync();
        }

        if (_serviceBusSql is not null)
        {
            await _serviceBusSql.DisposeAsync();
        }

        if (_sql is not null)
        {
            await _sql.DisposeAsync();
        }

        if (_apiImage is not null)
        {
            await _apiImage.DisposeAsync();
        }

        if (_webImage is not null)
        {
            await _webImage.DisposeAsync();
        }

        if (_webPublicImage is not null)
        {
            await _webPublicImage.DisposeAsync();
        }

        if (_network is not null)
        {
            await _network.DeleteAsync();
        }
    }
}
