namespace BikeBuilder.Contracts.Components;

public class DropperPostComponentInformation : ComponentInformation
{
  public static readonly double[] Diameters = [30.9, 31.6, 34.9];

  public override string DisplayName => "Dropper Post";

  public int TravelMm { get; set; }
  public double DiameterMm { get; set; }
}
