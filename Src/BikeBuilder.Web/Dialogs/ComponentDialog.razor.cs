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
  [Parameter] public string ComponentInformationJson { get; set; } = string.Empty;

  static readonly Manufacturer[] Manufacturers =
      [Manufacturer.Sram, Manufacturer.Shimano, Manufacturer.Hope, Manufacturer.Other];

  MudForm _form = null!;
  string _name = string.Empty;
  string _cost = string.Empty;
  string _description = string.Empty;
  string _sku = string.Empty;
  Manufacturer _manufacturer = Manufacturer.Other;
  ComponentInformation? _information;
  Type? _informationType;

  protected override void OnInitialized()
  {
    _name = Name;
    _cost = Cost;
    _description = Description;
    _sku = Sku;
    _manufacturer = Manufacturer;
    _information = ComponentInformationSerializer.TryDeserialize(ComponentInformationJson);
    _informationType = _information?.GetType();
  }

  void OnInformationTypeChanged(Type? type)
  {
    _informationType = type;
    if (type is null)
      _information = null;
    else if (_information?.GetType() != type)
      _information = (ComponentInformation)Activator.CreateInstance(type)!;
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

    MudDialog.Close(DialogResult.Ok(new ComponentDialogResult(_name, _cost, _description, _sku, _manufacturer, _information)));
  }

  void Cancel() => MudDialog.Cancel();
}
