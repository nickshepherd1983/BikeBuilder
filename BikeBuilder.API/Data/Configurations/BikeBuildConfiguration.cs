namespace BikeBuilder.API.Data.Configurations;

public class BikeBuildConfiguration : IEntityTypeConfiguration<BikeBuild>
{
    public void Configure(EntityTypeBuilder<BikeBuild> builder)
    {
        builder.ToTable("BikeBuilds");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Description)
            .HasMaxLength(2000);
    }
}
