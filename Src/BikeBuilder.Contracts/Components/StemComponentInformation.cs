namespace BikeBuilder.Contracts.Components;

public class StemComponentInformation : ComponentInformation
{
  public override string DisplayName => "Stem";

  public int LengthMm { get; set; }
}
