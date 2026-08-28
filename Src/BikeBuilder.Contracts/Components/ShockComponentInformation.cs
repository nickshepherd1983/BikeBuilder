namespace BikeBuilder.Contracts.Components;

public class ShockComponentInformation : ComponentInformation
{
  public override string DisplayName => "Shock";

  public int TravelMm { get; set; }
  public int StrokeMm { get; set; }
}
