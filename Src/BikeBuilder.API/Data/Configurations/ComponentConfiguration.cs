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

    // Polymorphic JSON column; the comparer snapshots via a JSON round trip because the
    // subtypes are mutable reference types edited in place - without it change tracking
    // would never see an in-place edit. The native json column type (SQL Server 2025+)
    // gets storage-level validation and binary storage; the converter still just reads and
    // writes the serialized string.
    builder.Property(c => c.Information)
        .HasColumnType("json")
        .HasConversion(
            information => ComponentInformationSerializer.Serialize(information),
            json => ComponentInformationSerializer.Deserialize(json),
            new ValueComparer<ComponentInformation>(
                (a, b) => ComponentInformationSerializer.Serialize(a) == ComponentInformationSerializer.Serialize(b),
                v => ComponentInformationSerializer.Serialize(v).GetHashCode(),
                v => ComponentInformationSerializer.Deserialize(ComponentInformationSerializer.Serialize(v))!));
  }
}
