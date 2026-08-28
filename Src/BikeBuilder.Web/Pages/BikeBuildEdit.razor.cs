namespace BikeBuilder.Web.Pages;

public partial class BikeBuildEdit(
    BikeBuildService.BikeBuildServiceClient _bikeBuildClient,
    RatingsClient _ratingsClient,
    IDialogService _dialogService,
    ISnackbar _snackbar,
    NavigationManager _navigation)
{
  [Parameter] public int Id { get; set; }

  // _bikeBuild (full components list from GetBikeBuild) stays the source of truth for the header
  // Total and the dialogs' recommended-max warning; only the grid display is server-paged.
  BikeBuildMessage? _bikeBuild;

  MudTable<BikeBuildComponentMessage> _componentsTable = null!;
  string _componentSearch = string.Empty;

  string _name = string.Empty;
  DateTime? _date = DateTime.Today;
  string _description = string.Empty;

  List<RatingDto>? _ratings;
  int _newRatingStars;
  string _newRatingComment = string.Empty;

  void GoBackToList() => _navigation.NavigateTo("/bikebuilds");

  static string FormatCost(string cost) =>
      decimal.TryParse(cost, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
          ? value.ToString("C2")
          : cost;

  protected override async Task OnInitializedAsync()
  {
    await LoadBikeBuild();
    await LoadRatings();
  }

  async Task LoadRatings()
  {
    try
    {
      _ratings = await _ratingsClient.ListAsync(Id);
    }
    catch (HttpRequestException)
    {
      _ratings = [];
      _snackbar.Add("Failed to load ratings.", Severity.Error);
    }
  }

  async Task SubmitRating()
  {
    if (_newRatingStars is < 1 or > 5)
    {
      _snackbar.Add("Pick 1 to 5 stars first.", Severity.Warning);
      return;
    }

    try
    {
      var comment = string.IsNullOrWhiteSpace(_newRatingComment) ? null : _newRatingComment;
      var response = await _ratingsClient.CreateAsync(Id, new CreateRatingRequest(_newRatingStars, comment, _bikeBuild!.Name));

      if (!response.IsSuccessStatusCode)
      {
        _snackbar.Add("Failed to submit rating.", Severity.Error);
        return;
      }

      _snackbar.Add("Rating submitted.", Severity.Success);
      _newRatingStars = 0;
      _newRatingComment = string.Empty;
      await LoadRatings();
    }
    catch (HttpRequestException)
    {
      _snackbar.Add("Failed to submit rating.", Severity.Error);
    }
  }

  async Task LoadBikeBuild()
  {
    _bikeBuild = await _bikeBuildClient.GetBikeBuildAsync(new GetBikeBuildRequest { Id = Id });
    _name = _bikeBuild.Name;
    _date = _bikeBuild.Date.ToDateTimeOffset().Date;
    _description = _bikeBuild.Description;
  }

  async Task<TableData<BikeBuildComponentMessage>> LoadBikeBuildComponentsAsync(TableState state, CancellationToken cancellationToken)
  {
    // MudTable pages are 0-based; the RPC is 1-based.
    var response = await _bikeBuildClient.ListBikeBuildComponentsAsync(new ListBikeBuildComponentsRequest
    {
      BikeBuildId = Id,
      Page = state.Page + 1,
      PageSize = state.PageSize,
      Search = _componentSearch,
      SortField = MapSortField(state),
      SortDescending = state.SortDirection == SortDirection.Descending
    }, cancellationToken: cancellationToken);

    return new TableData<BikeBuildComponentMessage> { Items = response.Components, TotalItems = response.TotalCount };
  }

  static BikeBuildComponentSortField MapSortField(TableState state)
  {
    // A third click on a sort label un-sorts (SortDirection.None) - fall back to the server default.
    if (state.SortDirection == SortDirection.None)
      return BikeBuildComponentSortField.Unspecified;

    return state.SortLabel switch
    {
      "component" => BikeBuildComponentSortField.ComponentName,
      "quantity" => BikeBuildComponentSortField.Quantity,
      "date" => BikeBuildComponentSortField.Date,
      _ => BikeBuildComponentSortField.Unspecified
    };
  }

  async Task ComponentSearchChanged(string _)
  {
    // Reset to the first page so filtering from a deep page doesn't strand on an empty page.
    _componentsTable.CurrentPage = 0;
    await _componentsTable.ReloadServerData();
  }

  async Task SaveBikeBuild()
  {
    if (_date is null)
      return;

    try
    {
      await _bikeBuildClient.UpdateBikeBuildAsync(new UpdateBikeBuildRequest
      {
        Id = Id,
        Name = _name,
        Date = Timestamp.FromDateTimeOffset(new DateTimeOffset(DateTime.SpecifyKind(_date.Value, DateTimeKind.Utc))),
        Description = _description
      });

      _snackbar.Add("Bike build saved.", Severity.Success);
      await LoadBikeBuild();
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }

  async Task AddBikeBuildComponent()
  {
    var parameters = new DialogParameters<BikeBuildComponentDialog>
    {
      { x => x.Title, "Add Component" },
      { x => x.ExistingComponents, (IReadOnlyList<BikeBuildComponentMessage>)_bikeBuild!.Components }
    };

    var dialog = await _dialogService.ShowAsync<BikeBuildComponentDialog>("Add Component", parameters);
    var result = await dialog.Result;

    if (result is null || result.Canceled)
      return;

    if (result.Data is not (int componentId, int quantity, DateTime componentDate))
      return;

    try
    {
      await _bikeBuildClient.AddBikeBuildComponentAsync(new AddBikeBuildComponentRequest
      {
        BikeBuildId = Id,
        ComponentId = componentId,
        Quantity = quantity,
        Date = Timestamp.FromDateTimeOffset(new DateTimeOffset(DateTime.SpecifyKind(componentDate, DateTimeKind.Utc)))
      });

      _snackbar.Add("Component added.", Severity.Success);
      await LoadBikeBuild();
      await _componentsTable.ReloadServerData();
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }

  async Task EditBikeBuildComponent(BikeBuildComponentMessage bbc)
  {
    var parameters = new DialogParameters<BikeBuildComponentDialog>
    {
      { x => x.Title, "Edit Component" },
      { x => x.ComponentId, bbc.ComponentId },
      { x => x.ComponentName, bbc.ComponentName },
      { x => x.ComponentInformationJson, bbc.ComponentInformationJson },
      { x => x.Quantity, bbc.Quantity },
      { x => x.Date, bbc.Date.ToDateTimeOffset().Date },
      { x => x.ExistingComponents, (IReadOnlyList<BikeBuildComponentMessage>)_bikeBuild!.Components },
      { x => x.ExcludeId, bbc.Id }
    };

    var dialog = await _dialogService.ShowAsync<BikeBuildComponentDialog>("Edit Component", parameters);
    var result = await dialog.Result;

    if (result is null || result.Canceled)
      return;

    if (result.Data is not (int componentId, int quantity, DateTime componentDate))
      return;

    try
    {
      await _bikeBuildClient.UpdateBikeBuildComponentAsync(new UpdateBikeBuildComponentRequest
      {
        Id = bbc.Id,
        ComponentId = componentId,
        Quantity = quantity,
        Date = Timestamp.FromDateTimeOffset(new DateTimeOffset(DateTime.SpecifyKind(componentDate, DateTimeKind.Utc)))
      });

      _snackbar.Add("Component updated.", Severity.Success);
      await LoadBikeBuild();
      await _componentsTable.ReloadServerData();
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }

  async Task RemoveBikeBuildComponent(BikeBuildComponentMessage bbc)
  {
    var confirmed = await _dialogService.ShowMessageBoxAsync(
        "Remove Component",
        $"Remove \"{bbc.ComponentName}\" from this bike build?",
        yesText: "Remove", cancelText: "Cancel");

    if (confirmed != true)
      return;

    try
    {
      await _bikeBuildClient.RemoveBikeBuildComponentAsync(new RemoveBikeBuildComponentRequest { Id = bbc.Id });
      _snackbar.Add("Component removed.", Severity.Success);
      await LoadBikeBuild();
      await _componentsTable.ReloadServerData();
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }
}
