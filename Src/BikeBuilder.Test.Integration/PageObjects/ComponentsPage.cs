namespace BikeBuilder.Test.Integration.PageObjects;

public class ComponentsPage(IPage page, string baseUrl)
{
  public Task GotoAsync() =>
      NavigationHelper.GotoAndWaitForHeadingAsync(page, $"{baseUrl}/components", "Components");

  public Task AddComponentAsync(string name, string cost, string description, string sku = "", string? manufacturer = null) => RetryHelper.RunAsync(async () =>
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

    if (sku.Length > 0)
      await dialog.GetByLabel("SKU").FillAsync(sku);

    if (manufacturer is not null)
    {
      // MudSelect renders both a hidden <input role="combobox"> and the visible <div
      // role="combobox"> sharing the same aria-label - scope to the div (see BikeBuildEditPage).
      await dialog.Locator("div[role='combobox'][aria-label='Manufacturer']").ClickAsync();
      await page.GetByRole(AriaRole.Option, new() { Name = manufacturer }).ClickAsync();
    }

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

  public async Task<bool> RowContainsAsync(string componentName, params string[] texts)
  {
    var rowText = await RowByName(componentName).InnerTextAsync();
    return texts.All(rowText.Contains);
  }

  ILocator RowByName(string componentName) =>
      page.Locator("table tbody tr").Filter(new() { HasText = componentName });
}
