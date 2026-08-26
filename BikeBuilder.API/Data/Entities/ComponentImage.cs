namespace BikeBuilder.API.Data.Entities;

public class ComponentImage
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;
    public string BlobName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}
