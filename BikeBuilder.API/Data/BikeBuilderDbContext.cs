namespace BikeBuilder.API.Data;

public class BikeBuilderDbContext(DbContextOptions<BikeBuilderDbContext> options) : DbContext(options)
{
    public DbSet<BikeBuild> BikeBuilds => Set<BikeBuild>();
    public DbSet<Component> Components => Set<Component>();
    public DbSet<BikeBuildComponent> BikeBuildComponents => Set<BikeBuildComponent>();
    public DbSet<ComponentImage> ComponentImages => Set<ComponentImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BikeBuilderDbContext).Assembly);
    }
}
