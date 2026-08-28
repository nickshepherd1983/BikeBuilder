namespace BikeBuilder.Test.Integration.PageObjects;

public class ComponentsPage(IPage page, string baseUrl)
{
  public Task GotoAsync() =>
      NavigationHelper.GotoAndWaitForHeadingAsync(page, $"{baseUrl}/components", "Components");

  public Task AddComponentAsync(string name, string cost, string description, string sku = "", string? manufacturer = null,
      string? informationType = null, IReadOnlyDictionary<string, string>? informationSelects = null) => RetryHelper.RunAsync(async () =>
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
      await ComboboxByLabel(dialog, "Manufacturer").ClickAsync();
      await page.GetByRole(AriaRole.Option, new() { Name = manufacturer }).ClickAsync();
    }

    if (informationType is not null)
    {
      await ComboboxByLabel(dialog, "Information Type").ClickAsync();
      await page.GetByRole(AriaRole.Option, new() { Name = informationType }).ClickAsync();

      foreach (var (label, value) in informationSelects ?? new Dictionary<string, string>())
      {
        await ComboboxByLabel(dialog, label).ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = value }).ClickAsync();
      }
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

  public async Task<ILocator> OpenEditDialogAsync(string componentName)
  {
    await RowByName(componentName).Locator("button[aria-label='Edit']").ClickAsync();
    var dialog = page.Locator(".mud-dialog");
    await dialog.WaitForAsync();
    return dialog;
  }

  public Task<string> GetInformationFieldTextAsync(ILocator dialog, string label) =>
      ComboboxByLabel(dialog, label).EvaluateAsync<string>("el => el.tagName === 'INPUT' ? el.value : el.innerText");

  // MudSelect renders a hidden <input role="combobox"> plus a visible <div role="combobox">
  // when the value matches a renderable MudSelectItem - but with no matching item (e.g. the
  // null "None" information type) the input itself is the visible combobox and no div exists,
  // so scope by visibility rather than element type (see BikeBuildEditPage for the old quirk).
  static ILocator ComboboxByLabel(ILocator dialog, string label) =>
      dialog.Locator($"[role='combobox'][aria-label='{label}']:visible");

  public async Task CancelDialogAsync(ILocator dialog)
  {
    await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
    await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
  }

  ILocator RowByName(string componentName) =>
      page.Locator("table tbody tr").Filter(new() { HasText = componentName });

  public ILocator Row(string componentName) => RowByName(componentName);

  // Debounced toolbar search - callers must use auto-retrying Expect waits afterwards.
  public Task SearchAsync(string term) => SearchField.FillAsync(term);

  public Task ClearSearchAsync() => SearchField.FillAsync(string.Empty);

  // Clicks a MudTableSortLabel by its visible column text (cycles asc -> desc -> unsorted).
  public Task SortByAsync(string column) =>
      page.Locator(".mud-table-sort-label", new() { HasText = column }).ClickAsync();

  ILocator SearchField => page.GetByPlaceholder("Search name or SKU");
}
