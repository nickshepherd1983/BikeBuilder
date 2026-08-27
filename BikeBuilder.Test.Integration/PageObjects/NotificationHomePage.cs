using static Microsoft.Playwright.Assertions;

namespace BikeBuilder.Test.Integration.PageObjects;

public class NotificationHomePage(IPage page, string baseUrl)
{
  public async Task GotoAsync()
  {
    await page.GotoAsync(baseUrl);
    await page.GetByRole(AriaRole.Heading, new() { Name = "BikeBuilder Live Activity" }).WaitForAsync();

    // That heading is present in the static-prerendered HTML, well before Blazor Server's
    // interactive circuit (and the real SignalR HubConnection Home.razor opens once
    // interactive) has finished connecting - settle briefly so callers can rely on the
    // notification hub connection actually being live once this method returns.
    await Task.Delay(TimeSpan.FromSeconds(2));
  }

  public Task WaitForNotificationAsync(string expectedTextSubstring, float timeout = 10_000) =>
      Expect(page.GetByRole(AriaRole.Alert).Filter(new() { HasText = expectedTextSubstring }))
          .ToBeVisibleAsync(new() { Timeout = timeout });
}
