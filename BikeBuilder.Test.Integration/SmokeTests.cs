using BikeBuilder.Test.Integration.PageObjects;

namespace BikeBuilder.Test.Integration;

[Collection("BikeBuilderApp")]
public class SmokeTests(BikeBuilderAppFixture fixture)
{
  [Fact]
  public async Task Can_create_components_upload_images_and_build_a_bike()
  {
    var page = await fixture.CreatePageAsync();
    var consoleMessages = new List<string>();
    page.Console += (_, msg) => consoleMessages.Add($"[{msg.Type}] {msg.Text}");
    page.PageError += (_, error) => consoleMessages.Add($"[pageerror] {error}");

    try
    {
      await RunScenarioAsync(page);
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
      await BikeBuilderAppFixture.SaveVideoAsync(page, "create-components-and-build");
    }
  }

  private async Task RunScenarioAsync(IPage page)
  {
    var components = new ComponentsPage(page, fixture.WebBaseAddress);
    var bikeBuilds = new BikeBuildsPage(page, fixture.WebBaseAddress);

    const string frameName = "Carbon Frame";
    const string brakesName = "Disc Brakes";

    await components.GotoAsync();
    await components.AddComponentAsync(frameName, "899.99", "Lightweight frame");
    await components.AddComponentAsync(brakesName, "129.50", "Hydraulic brakes");

    var imagePath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "test-image.png");
    await components.UploadImageToRowAsync(frameName, imagePath);
    await components.UploadImageToRowAsync(brakesName, imagePath);

    await bikeBuilds.GotoAsync();
    var editPage = await bikeBuilds.CreateBikeBuildAsync("Gravel Racer", "Test build");
    await editPage.AddComponentAsync(frameName, quantity: 1);
    await editPage.AddComponentAsync(brakesName, quantity: 2);

    var attached = await editPage.GetAttachedComponentNamesAsync();
    Assert.Contains(frameName, attached);
    Assert.Contains(brakesName, attached);

    await components.GotoAsync();
    Assert.True(await components.HasThumbnailAsync(frameName));
    Assert.True(await components.HasThumbnailAsync(brakesName));
  }

  [Fact]
  public async Task Can_create_components_build_a_bike_and_receive_a_live_notification()
  {
    var page = await fixture.CreatePageAsync();
    var notificationPage = await fixture.CreatePageAsync();
    var consoleMessages = new List<string>();
    page.Console += (_, msg) => consoleMessages.Add($"[{msg.Type}] {msg.Text}");
    page.PageError += (_, error) => consoleMessages.Add($"[pageerror] {error}");

    try
    {
      await RunNotificationScenarioAsync(page, notificationPage);
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
      await BikeBuilderAppFixture.SaveVideoAsync(page, "notification-app");
      await BikeBuilderAppFixture.SaveVideoAsync(notificationPage, "notification-toasts");
    }
  }

  [Fact]
  public async Task Can_leave_a_rating_and_see_the_toast()
  {
    var page = await fixture.CreatePageAsync();
    var notificationPage = await fixture.CreatePageAsync();
    var consoleMessages = new List<string>();
    page.Console += (_, msg) => consoleMessages.Add($"[{msg.Type}] {msg.Text}");
    page.PageError += (_, error) => consoleMessages.Add($"[pageerror] {error}");

    try
    {
      await RunRatingScenarioAsync(page, notificationPage);
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
      await BikeBuilderAppFixture.SaveVideoAsync(page, "rating-app");
      await BikeBuilderAppFixture.SaveVideoAsync(notificationPage, "rating-toasts");
    }
  }

  private async Task RunRatingScenarioAsync(IPage page, IPage notificationPage)
  {
    var components = new ComponentsPage(page, fixture.WebBaseAddress);
    var bikeBuilds = new BikeBuildsPage(page, fixture.WebBaseAddress);
    var notifications = new NotificationHomePage(notificationPage, fixture.WebPublicBaseAddress);

    const string forkName = "Suspension Fork";
    const string buildName = "Rated Ride";
    const string ratingComment = "Great climbing bike";

    await components.GotoAsync();
    await components.AddComponentAsync(forkName, "310.00", "120mm travel fork");

    await bikeBuilds.GotoAsync();
    var editPage = await bikeBuilds.CreateBikeBuildAsync(buildName, "Build for rating smoke test");
    await editPage.AddComponentAsync(forkName, quantity: 1);

    // Connect to Web.Public before submitting the rating so its SignalR connection is
    // already established and can't miss the notification.
    await notifications.GotoAsync();

    await editPage.AddRatingAsync(stars: 4, ratingComment);

    // The author name comes from the stub issuer's "name" claim for the test user.
    await editPage.WaitForRatingAsync(ratingComment, "Test User");
    await notifications.WaitForNotificationAsync($"New 4-star rating for {buildName}");
  }

  private async Task RunNotificationScenarioAsync(IPage page, IPage notificationPage)
  {
    var components = new ComponentsPage(page, fixture.WebBaseAddress);
    var bikeBuilds = new BikeBuildsPage(page, fixture.WebBaseAddress);
    var notifications = new NotificationHomePage(notificationPage, fixture.WebPublicBaseAddress);

    const string wheelName = "Aero Wheelset";
    const string saddleName = "Carbon Saddle";
    const string buildName = "Notification Test Ride";

    await components.GotoAsync();
    await components.AddComponentAsync(wheelName, "450.00", "Lightweight wheelset");
    await components.AddComponentAsync(saddleName, "120.00", "Carbon-railed saddle");

    var imagePath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "test-image.png");
    await components.UploadImageToRowAsync(wheelName, imagePath);
    await components.UploadImageToRowAsync(saddleName, imagePath);

    // Connect to Web.Public before creating the BikeBuild so its SignalR connection is
    // already established and can't miss the notification.
    await notifications.GotoAsync();

    await bikeBuilds.GotoAsync();
    var editPage = await bikeBuilds.CreateBikeBuildAsync(buildName, "Build for notification smoke test");

    // Only CreateBikeBuild publishes a notification event (not the per-component attach
    // calls below), so check for the toast right after creation.
    await notifications.WaitForNotificationAsync($"New bike build created: {buildName}");

    await editPage.AddComponentAsync(wheelName, quantity: 1);
    await editPage.AddComponentAsync(saddleName, quantity: 1);

    var attached = await editPage.GetAttachedComponentNamesAsync();
    Assert.Contains(wheelName, attached);
    Assert.Contains(saddleName, attached);
  }
}
