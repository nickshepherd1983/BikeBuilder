namespace BikeBuilder.Test.Integration.PageObjects;

static class NavigationHelper
{
  /// <summary>
  /// Navigates to <paramref name="url"/> and waits for <paramref name="expectedHeading"/> to
  /// appear. The heading itself renders as static markup regardless of whether the page's
  /// data-dependent gRPC call succeeds, so a short settle delay plus an explicit check of
  /// Blazor's fatal-render-error banner is needed to confirm the page actually loaded its data.
  /// </summary>
  public static async Task GotoAndWaitForHeadingAsync(IPage page, string url, string expectedHeading)
  {
    var started = DateTime.UtcNow;
    var consoleMessages = new List<string>();
    void OnConsole(object? _, IConsoleMessage message) => consoleMessages.Add($"[{message.Type}] {message.Text}");
    void OnPageError(object? _, string error) => consoleMessages.Add($"[pageerror] {error}");
    void OnFrameNavigated(object? _, IFrame frame)
    {
      if (frame.ParentFrame is null)
      {
        consoleMessages.Add($"[nav +{(DateTime.UtcNow - started).TotalSeconds:F1}s] {frame.Url}");
      }
    }
    page.Console += OnConsole;
    page.PageError += OnPageError;
    page.FrameNavigated += OnFrameNavigated;

    try
    {
      await page.GotoAsync(url);

      // The WASM app keeps its tokens in memory only, so every full navigation to a protected
      // page bounces through the stub OIDC issuer. In a fresh browser context that detour shows
      // the stub's Duende quickstart login form once; afterwards the stub's session cookie
      // SSOs straight through and the app lands on the requested page directly. Public pages
      // (Home, Web.Public) never redirect at all. Wait for whichever outcome materializes.
      var heading = page.GetByRole(AriaRole.Heading, new() { Name = expectedHeading });
      var usernameField = page.Locator("input[name='Input.Username']");
      await WaitWithDiagnosticsAsync(page, heading.Or(usernameField).First, "heading-or-login-form", url, consoleMessages);

      if (await usernameField.IsVisibleAsync())
      {
        await usernameField.FillAsync(BikeBuilderAppFixture.OidcTestUsername);
        await page.Locator("input[name='Input.Password']").FillAsync(BikeBuilderAppFixture.OidcTestPassword);
        await page.Locator("button[name='Input.Button'][value='login']").ClickAsync();
      }

      await WaitWithDiagnosticsAsync(page, heading, "post-login heading", url, consoleMessages);
    }
    finally
    {
      page.Console -= OnConsole;
      page.PageError -= OnPageError;
      page.FrameNavigated -= OnFrameNavigated;
    }

    await Task.Delay(TimeSpan.FromSeconds(1));

    if (await page.Locator("#blazor-error-ui").IsVisibleAsync())
    {
      throw new InvalidOperationException($"{url} showed the Blazor error banner after loading.");
    }
  }

  /// <summary>
  /// Waits for <paramref name="locator"/>, enriching a timeout with where the page actually
  /// ended up, the browser console, and a screenshot - the redirect chain through the stub
  /// OIDC issuer has too many moving parts to debug from a bare "element not found".
  /// </summary>
  static async Task WaitWithDiagnosticsAsync(IPage page, ILocator locator, string stage, string url, List<string> consoleMessages)
  {
    try
    {
      await locator.WaitForAsync();
    }
    catch (TimeoutException ex)
    {
      var screenshotPath = Path.Combine(Path.GetTempPath(), $"bikebuilder-nav-failure-{DateTime.UtcNow:HHmmss}.png");
      await page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
      var console = consoleMessages.Count > 0 ? string.Join(Environment.NewLine, consoleMessages) : "(no console output)";
      throw new InvalidOperationException(
          $"Navigating to {url} timed out waiting for {stage} at {page.Url}. Screenshot: {screenshotPath}{Environment.NewLine}Console:{Environment.NewLine}{console}", ex);
    }
  }
}
