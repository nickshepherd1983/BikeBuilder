namespace BikeBuilder.Contracts.Components;

public class TireComponentInformation : ComponentInformation
{
  public override string DisplayName => "Tire";

  // Nullable = not yet chosen; the editor marks both required.
  public WheelSize? Size { get; set; }
  public TireWidthInches? WidthInches { get; set; }
}
