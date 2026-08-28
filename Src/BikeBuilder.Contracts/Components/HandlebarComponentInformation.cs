namespace BikeBuilder.Contracts.Components;

public class HandlebarComponentInformation : ComponentInformation
{
  public override string DisplayName => "Handlebar";

  public HandlebarWidthMm WidthMm { get; set; } = new(780);
  public RiseMm RiseMm { get; set; } = new(20);
}
