namespace BikeBuilder.Contracts.Components;

public class StemComponentInformation : ComponentInformation
{
  public override string DisplayName => "Stem";

  public StemLengthMm LengthMm { get; set; } = new(50);
}
