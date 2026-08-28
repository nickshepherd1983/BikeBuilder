using BikeBuilder.Contracts.Types;

namespace BikeBuilder.Contracts.Components;

public class ForkComponentInformation : ComponentInformation
{
  public override string DisplayName => "Fork";

  public TravelMm TravelMm { get; set; } = new(150);

  public override IEnumerable<KeyValuePair<string, string>> GetDisplayValues()
  {
    yield return new("Travel", $"{TravelMm}mm");
  }
}
