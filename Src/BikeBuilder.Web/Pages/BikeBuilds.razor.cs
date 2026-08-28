namespace BikeBuilder.Web.Pages;

public partial class BikeBuilds(
    BikeBuildService.BikeBuildServiceClient _bikeBuildClient,
    RatingsClient _ratingsClient,
    IDialogService _dialogService,
    ISnackbar _snackbar,
    NavigationManager _navigation)
{
  // Same width treatment as the Components page's component dialog.
  static readonly DialogOptions _bikeBuildDialogOptions = new() { MaxWidth = MaxWidth.Small, FullWidth = true };

  MudTable<BikeBuildMessage> _table = null!;
  Dictionary<int, RatingSummaryDto>? _ratingSummaries;
  string _search = string.Empty;

  async Task<TableData<BikeBuildMessage>> LoadBikeBuildsAsync(TableState state, CancellationToken cancellationToken)
  {
    // MudTable pages are 0-based; the RPC is 1-based.
    var response = await _bikeBuildClient.ListBikeBuildsAsync(new ListBikeBuildsRequest
    {
      Page = state.Page + 1,
      PageSize = state.PageSize,
      Search = _search,
      SortField = MapSortField(state),
      SortDescending = state.SortDirection == SortDirection.Descending
    }, cancellationToken: cancellationToken);

    await LoadRatingSummaries(response.BikeBuilds.Select(b => b.Id));

    return new TableData<BikeBuildMessage> { Items = response.BikeBuilds, TotalItems = response.TotalCount };
  }

  static BikeBuildSortField MapSortField(TableState state)
  {
    // A third click on a sort label un-sorts (SortDirection.None) - fall back to the server default.
    if (state.SortDirection == SortDirection.None)
      return BikeBuildSortField.Unspecified;

    return state.SortLabel switch
    {
      "name" => BikeBuildSortField.Name,
      "date" => BikeBuildSortField.Date,
      "description" => BikeBuildSortField.Description,
      "total" => BikeBuildSortField.Total,
      _ => BikeBuildSortField.Unspecified
    };
  }

  async Task SearchChanged(string _)
  {
    // Reset to the first page so filtering from a deep page doesn't strand on an empty page.
    _table.CurrentPage = 0;
    await _table.ReloadServerData();
  }

  async Task LoadRatingSummaries(IEnumerable<int> bikeBuildIds)
  {
    try
    {
      _ratingSummaries = await _ratingsClient.GetSummariesAsync(bikeBuildIds);
    }
    catch (HttpRequestException)
    {
      // Ratings service unavailable - the grid still renders, ratings cells just stay blank.
      _ratingSummaries = null;
    }
  }

  string? RatingCountFor(int bikeBuildId)
  {
    if (_ratingSummaries is null)
      return null;

    var count = _ratingSummaries.TryGetValue(bikeBuildId, out var summary) ? summary.Count : 0;
    return count.ToString();
  }

  string? AverageRatingFor(int bikeBuildId) =>
      _ratingSummaries is not null && _ratingSummaries.TryGetValue(bikeBuildId, out var summary)
          ? summary.AverageStars.ToString("0.0", CultureInfo.InvariantCulture)
          : null;

  void EditBikeBuild(int id) => _navigation.NavigateTo($"/bikebuilds/{id}/edit");

  static string FormatCost(string cost) =>
      decimal.TryParse(cost, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
          ? value.ToString("C2")
          : cost;

  async Task CreateBikeBuild()
  {
    var parameters = new DialogParameters<BikeBuildDialog>
    {
      { x => x.Title, "Create Bike Build" }
    };

    var dialog = await _dialogService.ShowAsync<BikeBuildDialog>("Create Bike Build", parameters, _bikeBuildDialogOptions);
    var result = await dialog.Result;

    if (result is null || result.Canceled)
      return;

    if (result.Data is not (string name, DateTime date, string description))
      return;

    try
    {
      var created = await _bikeBuildClient.CreateBikeBuildAsync(new CreateBikeBuildRequest
      {
        Name = name,
        Date = Timestamp.FromDateTimeOffset(new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc))),
        Description = description
      });

      _navigation.NavigateTo($"/bikebuilds/{created.Id}/edit");
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }

  async Task DeleteBikeBuild(BikeBuildMessage bikeBuild)
  {
    var confirmed = await _dialogService.ShowMessageBoxAsync(
        "Delete Bike Build",
        $"Delete \"{bikeBuild.Name}\"? This will also remove its component assignments.",
        yesText: "Delete", cancelText: "Cancel");

    if (confirmed != true)
      return;

    try
    {
      await _bikeBuildClient.DeleteBikeBuildAsync(new DeleteBikeBuildRequest { Id = bikeBuild.Id });
      _snackbar.Add("Bike build deleted.", Severity.Success);
      await _table.ReloadServerData();
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }
}
