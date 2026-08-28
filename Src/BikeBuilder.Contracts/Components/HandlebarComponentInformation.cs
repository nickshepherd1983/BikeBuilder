namespace BikeBuilder.Contracts.Components;

public class HandlebarComponentInformation : ComponentInformation
{
  public override string DisplayName => "Handlebar";

  public int WidthMm { get; set; }
  public int RiseMm { get; set; }
}
