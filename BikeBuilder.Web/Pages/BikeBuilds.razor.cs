namespace BikeBuilder.Web.Pages;

public partial class BikeBuilds(
    BikeBuildService.BikeBuildServiceClient _bikeBuildClient,
    IDialogService _dialogService,
    ISnackbar _snackbar,
    NavigationManager _navigation)
{
  List<BikeBuildMessage>? _bikeBuilds;

  protected override async Task OnInitializedAsync()
  {
    await LoadBikeBuilds();
  }

  async Task LoadBikeBuilds()
  {
    var response = await _bikeBuildClient.ListBikeBuildsAsync(new ListBikeBuildsRequest());
    _bikeBuilds = response.BikeBuilds.ToList();
  }

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

    var dialog = await _dialogService.ShowAsync<BikeBuildDialog>("Create Bike Build", parameters);
    var result = await dialog.Result;

    if (result is null || result.Canceled)
      return;

    var (name, date, description) = ((string, DateTime, string))result.Data!;

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
      await LoadBikeBuilds();
    }
    catch (RpcException ex)
    {
      _snackbar.Add(ex.Status.Detail, Severity.Error);
    }
  }
}
