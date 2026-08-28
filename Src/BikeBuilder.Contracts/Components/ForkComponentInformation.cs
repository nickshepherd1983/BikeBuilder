namespace BikeBuilder.Contracts.Components;

public class ForkComponentInformation : ComponentInformation
{
  public override string DisplayName => "Fork";

  public int TravelMm { get; set; }
}
