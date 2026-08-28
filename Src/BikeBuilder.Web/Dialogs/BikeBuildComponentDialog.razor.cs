namespace BikeBuilder.Web.Dialogs;

public partial class BikeBuildComponentDialog(ComponentService.ComponentServiceClient _componentClient)
{
  [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

  [Parameter] public string Title { get; set; } = "Component";
  [Parameter] public int ComponentId { get; set; }
  [Parameter] public string ComponentName { get; set; } = string.Empty;
  [Parameter] public string ComponentInformationJson { get; set; } = string.Empty;
  [Parameter] public int Quantity { get; set; } = 1;
  [Parameter] public DateTime? Date { get; set; } = DateTime.Today;

  // The build's current rows, so the recommended-maximum warning can count how many of the
  // selected component's kind the build already carries. ExcludeId skips the row being
  // edited (0 when adding).
  [Parameter] public IReadOnlyList<BikeBuildComponentMessage> ExistingComponents { get; set; } = [];
  [Parameter] public int ExcludeId { get; set; }

  MudForm _form = null!;
  ComponentMessage? _component;
  int _quantity = 1;
  DateTime? _date = DateTime.Today;

  protected override void OnInitialized()
  {
    // Edit prefill without fetching: the autocomplete only needs Id + Name to display; the
    // information JSON keeps the recommendation warning accurate for the initial selection.
    if (ComponentId != 0)
      _component = new ComponentMessage { Id = ComponentId, Name = ComponentName, ComponentInformationJson = ComponentInformationJson };

    _quantity = Quantity;
    _date = Date;
  }

  // Recomputed each render (selection and quantity are bound); a recommendation only - the
  // Save button stays enabled.
  string? RecommendationWarning
  {
    get
    {
      var information = ComponentInformationSerializer.TryDeserialize(_component?.ComponentInformationJson);
      var recommendedMax = information?.GetRecommendedMaxPerBuild();
      if (information is null || recommendedMax is null)
        return null;

      var alreadyInBuild = ExistingComponents
          .Where(x => x.Id != ExcludeId)
          .Where(x => ComponentInformationSerializer.TryDeserialize(x.ComponentInformationJson)?.GetType() == information.GetType())
          .Sum(x => x.Quantity);
      var total = alreadyInBuild + _quantity;

      if (total <= recommendedMax)
        return null;

      var kind = recommendedMax == 1 ? information.DisplayName : $"{information.DisplayName}s";
      return $"Just so you know: a build usually has at most {recommendedMax} {kind}, and this would make it {total}. You can still save if that's what you intend.";
    }
  }

  async Task<IEnumerable<ComponentMessage>> SearchComponentsAsync(string search, CancellationToken cancellationToken)
  {
    var response = await _componentClient.ListComponentsAsync(new ListComponentsRequest
    {
      Search = search ?? string.Empty,
      Limit = 10
    }, cancellationToken: cancellationToken);

    return response.Components;
  }

  async Task Submit()
  {
    await _form.Validate();
    if (!_form.IsValid || _component is null || _date is null)
      return;

    MudDialog.Close(DialogResult.Ok((_component.Id, _quantity, _date.Value)));
  }

  void Cancel() => MudDialog.Cancel();
}
