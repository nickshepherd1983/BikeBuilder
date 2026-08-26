using Microsoft.Playwright;

namespace BikeBuilder.Test.Integration.PageObjects;

public class BikeBuildsPage(IPage page, string baseUrl)
{
    public Task GotoAsync() =>
        NavigationHelper.GotoAndWaitForHeadingAsync(page, $"{baseUrl}/bikebuilds", "Bike Builds");

    public async Task<BikeBuildEditPage> CreateBikeBuildAsync(string name, string description)
    {
        await RetryHelper.RunAsync(async () =>
        {
            var dialog = page.Locator(".mud-dialog");
            if (await dialog.IsVisibleAsync())
            {
                await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
                await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            }

            await page.GetByRole(AriaRole.Button, new() { Name = "Create Bike Build" }).ClickAsync();
            await dialog.GetByLabel("Name").FillAsync(name);
            await dialog.GetByLabel("Description").FillAsync(description);
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

            await page.GetByRole(AriaRole.Heading, new() { Name = "Edit Bike Build" }).WaitForAsync(new() { Timeout = 8000 });
        });

        return new BikeBuildEditPage(page);
    }
}
