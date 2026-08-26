namespace BikeBuilder.API.Data.Entities;

public class Component
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public string Description { get; set; } = string.Empty;

    public ComponentImage? Image { get; set; }

    public ICollection<BikeBuildComponent> BikeBuildComponents { get; set; } = new List<BikeBuildComponent>();
}
