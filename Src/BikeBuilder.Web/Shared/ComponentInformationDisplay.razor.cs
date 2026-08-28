namespace BikeBuilder.Web.Shared;

public partial class ComponentInformationDisplay
{
  [Parameter] public string Json { get; set; } = string.Empty;

  ComponentInformation? _information;
  string? _lastJson;

  protected override void OnParametersSet()
  {
    if (Json == _lastJson)
      return;

    _lastJson = Json;
    _information = ComponentInformationSerializer.TryDeserialize(Json);
  }
}
