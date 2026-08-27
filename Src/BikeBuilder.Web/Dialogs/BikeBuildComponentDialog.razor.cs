namespace BikeBuilder.Web.Dialogs;

public partial class BikeBuildComponentDialog(ComponentService.ComponentServiceClient _componentClient)
{
  [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

  [Parameter] public string Title { get; set; } = "Component";
  [Parameter] public int ComponentId { get; set; }
  [Parameter] public string ComponentName { get; set; } = string.Empty;
  [Parameter] public int Quantity { get; set; } = 1;
  [Parameter] public DateTime? Date { get; set; } = DateTime.Today;

  MudForm _form = null!;
  ComponentMessage? _component;
  int _quantity = 1;
  DateTime? _date = DateTime.Today;

  protected override void OnInitialized()
  {
    // Edit prefill without fetching: the autocomplete only needs Id + Name to display.
    if (ComponentId != 0)
      _component = new ComponentMessage { Id = ComponentId, Name = ComponentName };

    _quantity = Quantity;
    _date = Date;
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
