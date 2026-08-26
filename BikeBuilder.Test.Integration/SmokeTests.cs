using Microsoft.Playwright;
using BikeBuilder.Test.Integration.PageObjects;

namespace BikeBuilder.Test.Integration;

[Collection("BikeBuilderApp")]
public class SmokeTests(BikeBuilderAppFixture fixture)
{
    [Fact]
    public async Task Can_create_components_upload_images_and_build_a_bike()
    {
        var page = await fixture.Browser.NewPageAsync();
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
            await page.CloseAsync();
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
}
