namespace BikeBuilder.Contracts.Components;

public class ForkComponentInformation : ComponentInformation
{
  public override string DisplayName => "Fork";

  public TravelMm TravelMm { get; set; } = new(150);
}
