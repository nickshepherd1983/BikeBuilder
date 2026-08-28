namespace BikeBuilder.Contracts.Components;

public class TireComponentInformation : ComponentInformation
{
  public static readonly string[] Sizes = ["26", "27.5", "29"];
  public static readonly double[] Widths = [1.95, 2.0, 2.25, 2.4, 2.5, 2.6];

  public override string DisplayName => "Tire";

  public string Size { get; set; } = string.Empty;
  public double WidthInches { get; set; }
}
