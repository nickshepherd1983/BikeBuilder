namespace BikeBuilder.Test.Integration.PageObjects;

public class BikeBuildEditPage(IPage page)
{
  public Task AddComponentAsync(string componentName, int quantity) => RetryHelper.RunAsync(async () =>
  {
    var dialog = page.Locator(".mud-dialog");
    if (await dialog.IsVisibleAsync())
    {
      await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
      await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    await page.GetByRole(AriaRole.Button, new() { Name = "Add Component" }).ClickAsync();
    // MudSelect renders both a hidden <input role="combobox"> and the visible <div role="combobox">
    // sharing the same aria-label - GetByLabel/GetByRole alone match both. Scope to the div.
    await dialog.Locator("div[role='combobox'][aria-label='Component']").ClickAsync();
    await page.GetByRole(AriaRole.Option, new() { Name = componentName }).ClickAsync();
    await dialog.GetByLabel("Quantity").FillAsync(quantity.ToString());
    await dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

    await page.Locator("table tbody").GetByText(componentName, new() { Exact = true }).WaitForAsync(new() { Timeout = 8000 });
  });

  public async Task<IReadOnlyList<string>> GetAttachedComponentNamesAsync() =>
      await page.Locator("table tbody tr td:first-child").AllTextContentsAsync();
}
