namespace BikeBuilder.Contracts.Components;

public class ShockComponentInformation : ComponentInformation
{
  public override string DisplayName => "Shock";

  public TravelMm TravelMm { get; set; } = new(210);
  public StrokeMm StrokeMm { get; set; } = new(50);
}
