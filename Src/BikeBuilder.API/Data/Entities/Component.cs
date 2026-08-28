namespace BikeBuilder.API.Data.Entities;

public class Component
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public decimal Cost { get; set; }
  public string Description { get; set; } = string.Empty;
  public string Sku { get; set; } = string.Empty;
  public Manufacturer Manufacturer { get; set; } = Manufacturer.Other;
  public ComponentInformation? Information { get; set; }

  public ComponentImage? Image { get; set; }

  public ICollection<BikeBuildComponent> BikeBuildComponents { get; set; } = new List<BikeBuildComponent>();
}
