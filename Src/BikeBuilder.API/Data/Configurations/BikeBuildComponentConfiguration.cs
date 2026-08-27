namespace BikeBuilder.API.Data.Configurations;

public class BikeBuildComponentConfiguration : IEntityTypeConfiguration<BikeBuildComponent>
{
  public void Configure(EntityTypeBuilder<BikeBuildComponent> builder)
  {
    builder.ToTable("BikeBuildComponents");
    builder.HasKey(x => x.Id);

    builder.Property(x => x.Quantity)
        .IsRequired();

    builder.HasOne(x => x.BikeBuild)
        .WithMany(b => b.BikeBuildComponents)
        .HasForeignKey(x => x.BikeBuildId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(x => x.Component)
        .WithMany(c => c.BikeBuildComponents)
        .HasForeignKey(x => x.ComponentId)
        .OnDelete(DeleteBehavior.Restrict);
  }
}
