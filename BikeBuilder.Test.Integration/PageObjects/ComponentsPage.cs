namespace BikeBuilder.Test.Integration.PageObjects;

public class ComponentsPage(IPage page, string baseUrl)
{
    public Task GotoAsync() =>
        NavigationHelper.GotoAndWaitForHeadingAsync(page, $"{baseUrl}/components", "Components");

    public Task AddComponentAsync(string name, string cost, string description) => RetryHelper.RunAsync(async () =>
    {
        var dialog = page.Locator(".mud-dialog");
        if (await dialog.IsVisibleAsync())
        {
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
            await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Add Component" }).ClickAsync();
        await dialog.GetByLabel("Name").FillAsync(name);
        await dialog.GetByLabel("Cost").FillAsync(cost);
        await dialog.GetByLabel("Description").FillAsync(description);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await page.Locator("table tbody").GetByText(name, new() { Exact = true }).WaitForAsync(new() { Timeout = 8000 });
    });

    public Task UploadImageToRowAsync(string componentName, string filePath) => RetryHelper.RunAsync(async () =>
    {
        var row = RowByName(componentName);
        await row.Locator("input[type=file]").SetInputFilesAsync(filePath);
        await row.Locator("img").WaitForAsync(new() { Timeout = 8000 });
    });

    public async Task<bool> HasThumbnailAsync(string componentName)
    {
        var row = RowByName(componentName);
        return await row.Locator("td").First.Locator("img").CountAsync() > 0;
    }

    private ILocator RowByName(string componentName) =>
        page.Locator("table tbody tr").Filter(new() { HasText = componentName });
}
