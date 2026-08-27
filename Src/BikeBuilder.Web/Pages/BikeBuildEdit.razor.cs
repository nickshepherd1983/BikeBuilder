namespace BikeBuilder.Web.Pages;

public partial class BikeBuildEdit(
    BikeBuildService.BikeBuildServiceClient _bikeBuildClient,
    RatingsClient _ratingsClient,
    IDialogService _dialogService,
    ISnackbar _snackbar,
    NavigationManager _navigation)
{
  [Parameter] public int Id { get; set; }

  MudForm _form = null!;
  BikeBuildMessage? _bikeBuild;

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
      { x => x.Title, "Add Component" }
    };

    var dialog = await _dialogService.ShowAsync<BikeBuildComponentDialog>("Add Component", parameters);
    var result = await dialog.Result;

    if (result is null || result.Canceled)
      return;

    var (componentId, quantity, componentDate) = ((int, int, DateTime))result.Data!;

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
      { x => x.Quantity, bbc.Quantity },
      { x => x.Date, bbc.Date.ToDateTimeOffset().Date }
    };

    var dialog = await _dialogService.ShowAsync<BikeBuildComponentDialog>("Edit Component", parameters);
    var result = await dialog.Result;

    if (result is null || result.Canceled)
      return;

    var (componentId, quantity, componentDate) = ((int, int, DateTime))result.Data!;

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
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }
}
