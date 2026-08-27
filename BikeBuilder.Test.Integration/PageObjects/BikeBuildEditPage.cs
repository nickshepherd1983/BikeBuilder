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
    await Expect(RatingsSection.GetByText(comment)).ToBeVisibleAsync(new() { Timeout = 8000 });
    await Expect(RatingsSection.GetByText(userName)).ToBeVisibleAsync(new() { Timeout = 8000 });
  }

  ILocator RatingsSection => page.Locator(".mud-paper", new() { HasText = "Leave a rating" });
}
