namespace BikeBuilder.Test.Integration.PageObjects;

public class BikeBuildEditPage(IPage page)
{
  public Task AddComponentAsync(string componentName, int quantity, string? expectWarningContains = null) => RetryHelper.RunAsync(async () =>
  {
    var dialog = page.Locator(".mud-dialog");
    if (await dialog.IsVisibleAsync())
    {
      await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
      await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    await page.GetByRole(AriaRole.Button, new() { Name = "Add Component" }).ClickAsync();
    // Type into the autocomplete, then pick the matching suggestion from its popover.
    await dialog.GetByLabel("Component").FillAsync(componentName);
    await page.Locator(".mud-popover .mud-list-item").Filter(new() { HasText = componentName }).First.ClickAsync();
    await dialog.GetByLabel("Quantity").FillAsync(quantity.ToString());

    // The recommended-maximum warning is advisory: assert it shows, then save anyway.
    if (expectWarningContains is not null)
      await Expect(dialog.Locator(".mud-alert")).ToContainTextAsync(expectWarningContains, new() { Timeout = 8000 });

    await dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

    await page.Locator("table tbody").GetByText(componentName, new() { Exact = true }).WaitForAsync(new() { Timeout = 8000 });
  });

  public async Task<IReadOnlyList<string>> GetAttachedComponentNamesAsync() =>
      await page.Locator("table tbody tr td:first-child").AllTextContentsAsync();

  public ILocator ComponentRow(string componentName) =>
      page.Locator("table tbody tr").Filter(new() { HasText = componentName });

  // Columns: Component | Information | Quantity | Date | actions.
  public ILocator QuantityColumn => page.Locator("table tbody tr td:nth-child(3)");

  // Debounced toolbar search - callers must use auto-retrying Expect waits afterwards.
  public Task SearchComponentsAsync(string term) => SearchField.FillAsync(term);

  public Task ClearComponentSearchAsync() => SearchField.FillAsync(string.Empty);

  // Clicks a MudTableSortLabel by its visible column text (cycles asc -> desc -> unsorted).
  public Task SortByAsync(string column) =>
      page.Locator(".mud-table-sort-label", new() { HasText = column }).ClickAsync();

  ILocator SearchField => page.GetByPlaceholder("Search components");

  public async Task AddRatingAsync(int stars, string comment)
  {
    var section = RatingsSection;
    // MudRating renders as a role="radiogroup" span (no class of its own); each star's
    // accessible label sits on a hidden input behind the visible .mud-rating-item span,
    // which intercepts pointer events - so click the span itself. .Last picks the editable
    // picker, which sits below the read-only list ratings inside the same section.
    await section.GetByRole(AriaRole.Radiogroup).Last.Locator(".mud-rating-item").Nth(stars - 1).ClickAsync();
    await section.GetByLabel("Comment").FillAsync(comment);
    await section.GetByRole(AriaRole.Button, new() { Name = "Submit rating" }).ClickAsync();
  }

  public async Task WaitForRatingAsync(string comment, string userName)
  {
    // Multiple ratings by the same user can be on screen at once, so a bare
    // GetByText(userName) is a strict-mode violation from the second rating on. Anchor on
    // this rating's unique comment and check the author caption inside that entry's div.
    var entry = RatingsSection.GetByText(comment).Locator("..");
    await Expect(entry).ToBeVisibleAsync(new() { Timeout = 8000 });
    await Expect(entry.GetByText(userName)).ToBeVisibleAsync(new() { Timeout = 8000 });
  }

  ILocator RatingsSection => page.Locator(".mud-paper", new() { HasText = "Leave a rating" });
}
