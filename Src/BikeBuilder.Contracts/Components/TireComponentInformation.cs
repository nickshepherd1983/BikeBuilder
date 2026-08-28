using BikeBuilder.Contracts.Types;

namespace BikeBuilder.Contracts.Components;

public class TireComponentInformation : ComponentInformation
{
  public override string DisplayName => "Tire";

  // Nullable = not yet chosen; the editor marks both required.
  public WheelSize? Size { get; set; }
  public TireWidthInches? WidthInches { get; set; }

  public override IEnumerable<KeyValuePair<string, string>> GetDisplayValues()
  {
    if (Size is not null)
      yield return new("Size", $"{Size}\"");
    if (WidthInches is not null)
      yield return new("Width", $"{WidthInches}\"");
  }

  public override int? GetRecommendedMaxPerBuild() => 2;
}
