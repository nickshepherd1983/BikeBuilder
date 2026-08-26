using BikeBuilder.API.Services;

namespace BikeBuilder.API.Endpoints;

public static class ComponentImageEndpoints
{
    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif"
    };

    private const long MaxFileSize = 5_000_000;

    public static void MapComponentImageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/components/{id:int}/image", async (int id, IFormFile file,
            BikeBuilderDbContext db, ComponentImageStorageService storage, CancellationToken ct) =>
        {
            var extension = Path.GetExtension(file.FileName);
            if (!AllowedTypes.TryGetValue(extension, out var contentType))
            {
                return Results.BadRequest("Only .jpg, .jpeg, .png, and .gif files are allowed.");
            }

            if (file.Length is 0 or > MaxFileSize)
            {
                return Results.BadRequest("File must be between 1 byte and 5 MB.");
            }

            var component = await db.Components.Include(c => c.Image).FirstOrDefaultAsync(c => c.Id == id, ct);
            if (component is null)
            {
                return Results.NotFound();
            }

            await using var stream = file.OpenReadStream();
            var blobName = await storage.UploadAsync(id, extension, stream, contentType, ct);

            var oldBlobName = component.Image?.BlobName;
            var uploadedAt = DateTimeOffset.UtcNow;

            if (component.Image is { } existing)
            {
                existing.BlobName = blobName;
                existing.ContentType = contentType;
                existing.OriginalFileName = file.FileName;
                existing.UploadedAt = uploadedAt;
            }
            else
            {
                component.Image = new ComponentImage
                {
                    BlobName = blobName,
                    ContentType = contentType,
                    OriginalFileName = file.FileName,
                    UploadedAt = uploadedAt
                };
            }

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch
            {
                await storage.DeleteAsync(blobName, ct);
                throw;
            }

            if (oldBlobName is not null)
            {
                await storage.DeleteAsync(oldBlobName, ct);
            }

            return Results.Ok(new { hasImage = true, imageVersion = uploadedAt.UtcTicks });
        }).DisableAntiforgery();

        app.MapDelete("/api/components/{id:int}/image", async (int id,
            BikeBuilderDbContext db, ComponentImageStorageService storage, CancellationToken ct) =>
        {
            var component = await db.Components.Include(c => c.Image).FirstOrDefaultAsync(c => c.Id == id, ct);
            if (component?.Image is null)
            {
                return Results.NotFound();
            }

            var blobName = component.Image.BlobName;
            db.ComponentImages.Remove(component.Image);
            await db.SaveChangesAsync(ct);
            await storage.DeleteAsync(blobName, ct);

            return Results.NoContent();
        });

        app.MapGet("/api/components/{id:int}/image", async (int id,
            BikeBuilderDbContext db, ComponentImageStorageService storage, HttpResponse response, CancellationToken ct) =>
        {
            var image = await db.ComponentImages.AsNoTracking().FirstOrDefaultAsync(x => x.ComponentId == id, ct);
            if (image is null)
            {
                return Results.NotFound();
            }

            var (stream, contentType) = await storage.OpenReadAsync(image.BlobName, image.ContentType, ct);
            response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.Stream(stream, contentType);
        });
    }
}
