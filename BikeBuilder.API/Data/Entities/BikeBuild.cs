namespace BikeBuilder.API.Data.Entities;

public class BikeBuild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string Description { get; set; } = string.Empty;

    public ICollection<BikeBuildComponent> BikeBuildComponents { get; set; } = new List<BikeBuildComponent>();
}
