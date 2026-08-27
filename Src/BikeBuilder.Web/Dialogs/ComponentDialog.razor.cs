namespace BikeBuilder.Web.Dialogs;

public partial class ComponentDialog
{
  [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

  [Parameter] public string Title { get; set; } = "Component";
  [Parameter] public string Name { get; set; } = string.Empty;
  [Parameter] public string Cost { get; set; } = string.Empty;
  [Parameter] public string Description { get; set; } = string.Empty;
  [Parameter] public string Sku { get; set; } = string.Empty;
  [Parameter] public Manufacturer Manufacturer { get; set; } = Manufacturer.Other;

  static readonly Manufacturer[] Manufacturers =
      [Manufacturer.Sram, Manufacturer.Shimano, Manufacturer.Hope, Manufacturer.Other];

  MudForm _form = null!;
  string _name = string.Empty;
  string _cost = string.Empty;
  string _description = string.Empty;
  string _sku = string.Empty;
  Manufacturer _manufacturer = Manufacturer.Other;

  protected override void OnInitialized()
  {
    _name = Name;
    _cost = Cost;
    _description = Description;
    _sku = Sku;
    _manufacturer = Manufacturer;
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

    MudDialog.Close(DialogResult.Ok((_name, _cost, _description, _sku, _manufacturer)));
  }

  void Cancel() => MudDialog.Cancel();
}
