namespace BikeBuilder.Test.Integration;

[Collection("BikeBuilderApp")]
public class SmokeTests(BikeBuilderAppFixture fixture)
{
  [Fact]
  public async Task Can_create_component_with_image_build_bike_rate_it_and_see_notifications()
  {
    var page = await fixture.CreatePageAsync();
    var notificationPage = await fixture.CreatePageAsync();
    var consoleMessages = new List<string>();
    page.Console += (_, msg) => consoleMessages.Add($"[{msg.Type}] {msg.Text}");
    page.PageError += (_, error) => consoleMessages.Add($"[pageerror] {error}");

    try
    {
      await RunScenarioAsync(page, notificationPage);
    }
    catch
    {
      var resultsDir = Path.Combine(AppContext.BaseDirectory, "TestResults");
      Directory.CreateDirectory(resultsDir);
      var id = Guid.NewGuid().ToString("N");
      await page.ScreenshotAsync(new() { Path = Path.Combine(resultsDir, $"failure-{id}.png"), FullPage = true });
      await File.WriteAllLinesAsync(Path.Combine(resultsDir, $"failure-{id}-console.log"), consoleMessages);
      throw;
    }
    finally
    {
      await BikeBuilderAppFixture.SaveVideoAsync(page, "full-smoke-app");
      await BikeBuilderAppFixture.SaveVideoAsync(notificationPage, "full-smoke-toasts");
    }
  }

  async Task RunScenarioAsync(IPage page, IPage notificationPage)
  {
    var components = new ComponentsPage(page, fixture.WebBaseAddress);
    var bikeBuilds = new BikeBuildsPage(page, fixture.WebBaseAddress);
    var notifications = new NotificationHomePage(notificationPage, fixture.WebPublicBaseAddress);

    // The fixture pre-seeds 1000+ components and the grid is paginated in name order, so
    // the test component's name must sort ahead of every seeded brand to stay on page 1.
    const string frameName = "AAA Carbon Frame";
    const string buildName = "Full Smoke Ride";

    // First navigation drives the stub OIDC login flow - this is the "log in" step.
    await components.GotoAsync();
    await components.AddComponentAsync(frameName, "899.99", "Lightweight frame", sku: "CF-1001", manufacturer: "Hope");
    Assert.True(await components.RowContainsAsync(frameName, "CF-1001", "Hope"));

    var imagePath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "test-image.png");
    await components.UploadImageToRowAsync(frameName, imagePath);
    Assert.True(await components.HasThumbnailAsync(frameName));

    // Connect to Web.Public before creating the BikeBuild so its SignalR connection is
    // already established and can't miss any of the notifications below.
    await notifications.GotoAsync();

    await bikeBuilds.GotoAsync();
    var editPage = await bikeBuilds.CreateBikeBuildAsync(buildName, "Build for full smoke test");

    // Only CreateBikeBuild publishes a notification event (not the per-component attach
    // calls below), so check for the toast right after creation.
    await notifications.WaitForNotificationAsync($"New bike build created: {buildName}");

    await editPage.AddComponentAsync(frameName, quantity: 1);
    var attached = await editPage.GetAttachedComponentNamesAsync();
    Assert.Contains(frameName, attached);

    // Check each rating's toast right after submitting it - snackbar toasts auto-dismiss,
    // so batching the checks at the end would race the first toast's timeout.
    // The author name comes from the stub issuer's "name" claim for the test user.
    await editPage.AddRatingAsync(stars: 4, "Great climbing bike");
    await editPage.WaitForRatingAsync("Great climbing bike", "Test User");
    await notifications.WaitForNotificationAsync($"New 4-star rating for {buildName}");

    await editPage.AddRatingAsync(stars: 5, "Even better downhill");
    await editPage.WaitForRatingAsync("Even better downhill", "Test User");
    await notifications.WaitForNotificationAsync($"New 5-star rating for {buildName}");

    // Back on the grid, the Ratings column should show both ratings and the Average column
    // their mean (4 and 5 stars). Expect polls, so the async summary fetch after the grid
    // renders can't race these assertions.
    await bikeBuilds.GotoAsync();
    await Expect(bikeBuilds.RatingsCountCell(buildName)).ToHaveTextAsync("2");
    await Expect(bikeBuilds.AverageRatingCell(buildName)).ToHaveTextAsync("4.5");
    await Expect(bikeBuilds.Pager).ToBeVisibleAsync();
  }
}
