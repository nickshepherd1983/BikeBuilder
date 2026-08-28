namespace BikeBuilder.Contracts.Components;

public class RimComponentInformation : ComponentInformation
{
  public override string DisplayName => "Rim";

  // Nullable = not yet chosen; the editor marks it required.
  public WheelSize? Size { get; set; }
}
