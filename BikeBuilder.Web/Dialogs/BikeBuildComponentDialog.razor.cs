namespace BikeBuilder.Web.Dialogs;

public partial class BikeBuildComponentDialog
{
  [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

  [Parameter] public string Title { get; set; } = "Component";
  [Parameter] public List<ComponentMessage> AllComponents { get; set; } = [];
  [Parameter] public int ComponentId { get; set; }
  [Parameter] public int Quantity { get; set; } = 1;
  [Parameter] public DateTime? Date { get; set; } = DateTime.Today;

  MudForm _form = null!;
  int _componentId;
  int _quantity = 1;
  DateTime? _date = DateTime.Today;

  protected override void OnInitialized()
  {
    _componentId = ComponentId != 0 ? ComponentId : AllComponents.Count > 0 ? AllComponents[0].Id : 0;
    _quantity = Quantity;
    _date = Date;
  }

  async Task Submit()
  {
    await _form.Validate();
    if (!_form.IsValid || _date is null)
      return;

    MudDialog.Close(DialogResult.Ok((_componentId, _quantity, _date.Value)));
  }

  void Cancel() => MudDialog.Cancel();
}
