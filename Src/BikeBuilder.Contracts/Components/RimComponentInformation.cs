namespace BikeBuilder.Contracts.Components;

public class RimComponentInformation : ComponentInformation
{
  public static readonly string[] Sizes = ["26", "27.5", "29"];

  public override string DisplayName => "Rim";

  public string Size { get; set; } = string.Empty;
}
