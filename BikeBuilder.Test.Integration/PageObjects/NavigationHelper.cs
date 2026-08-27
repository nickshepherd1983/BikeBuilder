namespace BikeBuilder.Test.Integration.PageObjects;

internal static class NavigationHelper
{
  /// <summary>
  /// Navigates to <paramref name="url"/> and waits for <paramref name="expectedHeading"/> to
  /// appear. The heading itself renders as static markup regardless of whether the page's
  /// data-dependent gRPC call succeeds, so a short settle delay plus an explicit check of
  /// Blazor's fatal-render-error banner is needed to confirm the page actually loaded its data.
  /// </summary>
  public static async Task GotoAndWaitForHeadingAsync(IPage page, string url, string expectedHeading)
  {
    await page.GotoAsync(url);
    await page.GetByRole(AriaRole.Heading, new() { Name = expectedHeading }).WaitForAsync();

    await Task.Delay(TimeSpan.FromSeconds(1));

    if (await page.Locator("#blazor-error-ui").IsVisibleAsync())
    {
      throw new InvalidOperationException($"{url} showed the Blazor error banner after loading.");
    }
  }
}
