namespace BikeBuilder.API.Data.Configurations;

public class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
  public void Configure(EntityTypeBuilder<Component> builder)
  {
    builder.ToTable("Components");
    builder.HasKey(c => c.Id);

    builder.Property(c => c.Name)
        .IsRequired()
        .HasMaxLength(200);

    builder.Property(c => c.Cost)
        .HasColumnType("decimal(18,2)");

    builder.Property(c => c.Description)
        .HasMaxLength(2000);

    builder.Property(c => c.Sku)
        .HasMaxLength(100);

    // Stored as its name; the default matters for rows that predate the column - an empty
    // string would fail the enum conversion on read.
    builder.Property(c => c.Manufacturer)
        .HasConversion<string>()
        .HasMaxLength(20)
        .HasDefaultValue(Entities.Manufacturer.Other);
  }
}
