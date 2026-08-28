namespace BikeBuilder.Contracts.Components;

public class DropperPostComponentInformation : ComponentInformation
{
  public override string DisplayName => "Dropper Post";

  public TravelMm TravelMm { get; set; } = new(150);
  public SeatpostDiameterMm DiameterMm { get; set; } = new(31.6);
}
