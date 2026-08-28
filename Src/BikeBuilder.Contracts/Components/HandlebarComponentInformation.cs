using BikeBuilder.Contracts.Types;

namespace BikeBuilder.Contracts.Components;

public class HandlebarComponentInformation : ComponentInformation
{
  public override string DisplayName => "Handlebar";

  public HandlebarWidthMm WidthMm { get; set; } = new(780);
  public RiseMm RiseMm { get; set; } = new(20);

  public override IEnumerable<KeyValuePair<string, string>> GetDisplayValues()
  {
    yield return new("Width", $"{WidthMm}mm");
    yield return new("Rise", $"{RiseMm}mm");
  }

  public override int? GetRecommendedMaxPerBuild() => 1;
}
