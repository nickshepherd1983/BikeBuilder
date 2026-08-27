namespace BikeBuilder.Web.Dialogs;

public partial class ComponentDialog
{
  [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

  [Parameter] public string Title { get; set; } = "Component";
  [Parameter] public string Name { get; set; } = string.Empty;
  [Parameter] public string Cost { get; set; } = string.Empty;
  [Parameter] public string Description { get; set; } = string.Empty;

  MudForm _form = null!;
  string _name = string.Empty;
  string _cost = string.Empty;
  string _description = string.Empty;

  protected override void OnInitialized()
  {
    _name = Name;
    _cost = Cost;
    _description = Description;
  }

  static string? ValidateCost(string value) =>
      decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
          ? null
          : "Enter a valid number";

  async Task Submit()
  {
    await _form.Validate();
    if (!_form.IsValid)
      return;

    MudDialog.Close(DialogResult.Ok((_name, _cost, _description)));
  }

  void Cancel() => MudDialog.Cancel();
}
