using BikeBuilder.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BikeBuilder.API.Data.Configurations;

public class ComponentImageConfiguration : IEntityTypeConfiguration<ComponentImage>
{
    public void Configure(EntityTypeBuilder<ComponentImage> builder)
    {
        builder.ToTable("ComponentImages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BlobName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.OriginalFileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.HasIndex(x => x.ComponentId).IsUnique();

        // Cascade here (unlike BikeBuildComponent's Restrict on Component) because the image
        // row has no meaning without its Component; the blob itself is cleaned up in app code.
        builder.HasOne(x => x.Component)
            .WithOne(c => c.Image)
            .HasForeignKey<ComponentImage>(x => x.ComponentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
