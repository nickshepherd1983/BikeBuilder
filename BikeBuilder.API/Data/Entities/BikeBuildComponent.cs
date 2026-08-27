namespace BikeBuilder.API.Data.Entities;

public class BikeBuildComponent
{
  public int Id { get; set; }

  public int BikeBuildId { get; set; }
  public BikeBuild BikeBuild { get; set; } = null!;

  public int ComponentId { get; set; }
  public Component Component { get; set; } = null!;

  public int Quantity { get; set; }
  public DateTimeOffset Date { get; set; }
}
