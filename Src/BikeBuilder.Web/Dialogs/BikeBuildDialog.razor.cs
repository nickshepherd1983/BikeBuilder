namespace BikeBuilder.Web.Dialogs;

public partial class BikeBuildDialog
{
  [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

  [Parameter] public string Title { get; set; } = "Bike Build";
  [Parameter] public string Name { get; set; } = string.Empty;
  [Parameter] public DateTime? Date { get; set; } = DateTime.Today;
  [Parameter] public string Description { get; set; } = string.Empty;

  MudForm _form = null!;
  string _name = string.Empty;
  DateTime? _date = DateTime.Today;
  string _description = string.Empty;

  protected override void OnInitialized()
  {
    _name = Name;
    _date = Date;
    _description = Description;
  }

  async Task Submit()
  {
    await _form.Validate();
    if (!_form.IsValid || _date is null)
      return;

    MudDialog.Close(DialogResult.Ok((_name, _date.Value, _description)));
  }

  void Cancel() => MudDialog.Cancel();
}
