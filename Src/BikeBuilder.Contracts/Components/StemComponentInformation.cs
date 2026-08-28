using BikeBuilder.Contracts.Types;

namespace BikeBuilder.Contracts.Components;

public class StemComponentInformation : ComponentInformation
{
  public override string DisplayName => "Stem";

  public StemLengthMm LengthMm { get; set; } = new(50);

  public override IEnumerable<KeyValuePair<string, string>> GetDisplayValues()
  {
    yield return new("Length", $"{LengthMm}mm");
  }

  public override int? GetRecommendedMaxPerBuild() => 1;
}
