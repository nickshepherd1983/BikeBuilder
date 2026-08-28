using Google.Protobuf.WellKnownTypes;

namespace BikeBuilder.API.Services;

public class BikeBuildGrpcService(BikeBuilderDbContext db, IEventPublisher eventPublisher) : BikeBuildService.BikeBuildServiceBase
{
  public override async Task<ListBikeBuildsResponse> ListBikeBuilds(ListBikeBuildsRequest request, ServerCallContext context)
  {
    var page = Math.Max(request.Page, 1);
    var pageSize = Math.Clamp(request.PageSize <= 0 ? 20 : request.PageSize, 1, 100);

    IQueryable<Data.Entities.BikeBuild> query = db.BikeBuilds
        .Include(b => b.BikeBuildComponents)
        .ThenInclude(x => x.Component)
        .AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Search))
      query = query.Where(b => b.Name.Contains(request.Search) || b.Description.Contains(request.Search));

    var totalCount = await query.CountAsync(context.CancellationToken);

    // (Date, true) falls through to the default arm - same ordering.
    query = (request.SortField, request.SortDescending) switch
    {
      (BikeBuildSortField.Name, false) => query.OrderBy(b => b.Name).ThenBy(b => b.Id),
      (BikeBuildSortField.Name, true) => query.OrderByDescending(b => b.Name).ThenByDescending(b => b.Id),
      (BikeBuildSortField.Date, false) => query.OrderBy(b => b.Date).ThenBy(b => b.Id),
      (BikeBuildSortField.Description, false) => query.OrderBy(b => b.Description).ThenBy(b => b.Id),
      (BikeBuildSortField.Description, true) => query.OrderByDescending(b => b.Description).ThenByDescending(b => b.Id),
      (BikeBuildSortField.Total, false) => query.OrderBy(b => b.BikeBuildComponents.Sum(x => x.Component.Cost * x.Quantity)).ThenBy(b => b.Id),
      (BikeBuildSortField.Total, true) => query.OrderByDescending(b => b.BikeBuildComponents.Sum(x => x.Component.Cost * x.Quantity)).ThenByDescending(b => b.Id),
      _ => query.OrderByDescending(b => b.Date).ThenByDescending(b => b.Id)
    };

    var bikeBuilds = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(context.CancellationToken);

    var response = new ListBikeBuildsResponse { TotalCount = totalCount };
    response.BikeBuilds.AddRange(bikeBuilds.Select(b => ToMessage(b, includeComponents: false)));
    return response;
  }

  public override async Task<BikeBuildMessage> GetBikeBuild(GetBikeBuildRequest request, ServerCallContext context)
  {
    var bikeBuild = await LoadBikeBuildWithComponents(request.Id, context.CancellationToken);
    return ToMessage(bikeBuild, includeComponents: true);
  }

  public override async Task<BikeBuildMessage> CreateBikeBuild(CreateBikeBuildRequest request, ServerCallContext context)
  {
    var bikeBuild = new Data.Entities.BikeBuild
    {
      Name = request.Name,
      Date = request.Date.ToDateTimeOffset(),
      Description = request.Description
    };

    db.BikeBuilds.Add(bikeBuild);
    await db.SaveChangesAsync(context.CancellationToken);

    await eventPublisher.PublishAsync(ServiceBusMessageTypes.BikeBuildCreated,
        new BikeBuildCreatedEvent
        {
          Id = bikeBuild.Id,
          Name = bikeBuild.Name,
          CreatedAt = DateTimeOffset.UtcNow
        },
        context.CancellationToken);

    return ToMessage(bikeBuild, includeComponents: false);
  }

  public override async Task<BikeBuildMessage> UpdateBikeBuild(UpdateBikeBuildRequest request, ServerCallContext context)
  {
    var bikeBuild = await db.BikeBuilds.FirstOrDefaultAsync(b => b.Id == request.Id, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"BikeBuild {request.Id} not found."));

    bikeBuild.Name = request.Name;
    bikeBuild.Date = request.Date.ToDateTimeOffset();
    bikeBuild.Description = request.Description;

    await db.SaveChangesAsync(context.CancellationToken);

    return ToMessage(bikeBuild, includeComponents: false);
  }

  public override async Task<DeleteBikeBuildResponse> DeleteBikeBuild(DeleteBikeBuildRequest request, ServerCallContext context)
  {
    var bikeBuild = await db.BikeBuilds.FirstOrDefaultAsync(b => b.Id == request.Id, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"BikeBuild {request.Id} not found."));

    db.BikeBuilds.Remove(bikeBuild);
    await db.SaveChangesAsync(context.CancellationToken);

    return new DeleteBikeBuildResponse { Success = true };
  }

  public override async Task<BikeBuildComponentMessage> AddBikeBuildComponent(AddBikeBuildComponentRequest request, ServerCallContext context)
  {
    var bikeBuildExists = await db.BikeBuilds.AnyAsync(b => b.Id == request.BikeBuildId, context.CancellationToken);
    if (!bikeBuildExists)
    {
      throw new RpcException(new Status(StatusCode.NotFound, $"BikeBuild {request.BikeBuildId} not found."));
    }

    var component = await db.Components.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ComponentId, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"Component {request.ComponentId} not found."));

    var bikeBuildComponent = new Data.Entities.BikeBuildComponent
    {
      BikeBuildId = request.BikeBuildId,
      ComponentId = request.ComponentId,
      Quantity = request.Quantity,
      Date = request.Date.ToDateTimeOffset()
    };

    db.BikeBuildComponents.Add(bikeBuildComponent);
    await db.SaveChangesAsync(context.CancellationToken);

    return ToMessage(bikeBuildComponent, component);
  }

  public override async Task<BikeBuildComponentMessage> UpdateBikeBuildComponent(UpdateBikeBuildComponentRequest request, ServerCallContext context)
  {
    var bikeBuildComponent = await db.BikeBuildComponents.FirstOrDefaultAsync(x => x.Id == request.Id, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"BikeBuildComponent {request.Id} not found."));

    var component = await db.Components.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ComponentId, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"Component {request.ComponentId} not found."));

    bikeBuildComponent.ComponentId = request.ComponentId;
    bikeBuildComponent.Quantity = request.Quantity;
    bikeBuildComponent.Date = request.Date.ToDateTimeOffset();

    await db.SaveChangesAsync(context.CancellationToken);

    return ToMessage(bikeBuildComponent, component);
  }

  public override async Task<RemoveBikeBuildComponentResponse> RemoveBikeBuildComponent(RemoveBikeBuildComponentRequest request, ServerCallContext context)
  {
    var bikeBuildComponent = await db.BikeBuildComponents.FirstOrDefaultAsync(x => x.Id == request.Id, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"BikeBuildComponent {request.Id} not found."));

    db.BikeBuildComponents.Remove(bikeBuildComponent);
    await db.SaveChangesAsync(context.CancellationToken);

    return new RemoveBikeBuildComponentResponse { Success = true };
  }

  public override async Task<ListBikeBuildComponentsResponse> ListBikeBuildComponents(ListBikeBuildComponentsRequest request, ServerCallContext context)
  {
    var bikeBuildExists = await db.BikeBuilds.AnyAsync(b => b.Id == request.BikeBuildId, context.CancellationToken);
    if (!bikeBuildExists)
      throw new RpcException(new Status(StatusCode.NotFound, $"BikeBuild {request.BikeBuildId} not found."));

    var page = Math.Max(request.Page, 1);
    var pageSize = Math.Clamp(request.PageSize <= 0 ? 20 : request.PageSize, 1, 100);

    IQueryable<Data.Entities.BikeBuildComponent> query = db.BikeBuildComponents
        .Include(x => x.Component)
        .AsNoTracking()
        .Where(x => x.BikeBuildId == request.BikeBuildId);

    if (!string.IsNullOrWhiteSpace(request.Search))
      query = query.Where(x => x.Component.Name.Contains(request.Search));

    var totalCount = await query.CountAsync(context.CancellationToken);

    query = (request.SortField, request.SortDescending) switch
    {
      (BikeBuildComponentSortField.ComponentName, false) => query.OrderBy(x => x.Component.Name).ThenBy(x => x.Id),
      (BikeBuildComponentSortField.ComponentName, true) => query.OrderByDescending(x => x.Component.Name).ThenByDescending(x => x.Id),
      (BikeBuildComponentSortField.Quantity, false) => query.OrderBy(x => x.Quantity).ThenBy(x => x.Id),
      (BikeBuildComponentSortField.Quantity, true) => query.OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Id),
      (BikeBuildComponentSortField.Date, false) => query.OrderBy(x => x.Date).ThenBy(x => x.Id),
      (BikeBuildComponentSortField.Date, true) => query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id),
      // UNSPECIFIED -> insertion order, matching the old in-memory grid.
      _ => query.OrderBy(x => x.Id)
    };

    var rows = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(context.CancellationToken);

    var response = new ListBikeBuildComponentsResponse { TotalCount = totalCount };
    response.Components.AddRange(rows.Select(x => ToMessage(x, x.Component)));
    return response;
  }

  async Task<Data.Entities.BikeBuild> LoadBikeBuildWithComponents(int id, CancellationToken cancellationToken)
  {
    return await db.BikeBuilds
        .Include(b => b.BikeBuildComponents)
        .ThenInclude(x => x.Component)
        .AsNoTracking()
        .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"BikeBuild {id} not found."));
  }

  static BikeBuildMessage ToMessage(Data.Entities.BikeBuild bikeBuild, bool includeComponents)
  {
    var message = new BikeBuildMessage
    {
      Id = bikeBuild.Id,
      Name = bikeBuild.Name,
      Date = Timestamp.FromDateTimeOffset(bikeBuild.Date),
      Description = bikeBuild.Description,
      Total = bikeBuild.BikeBuildComponents.Sum(x => x.Component.Cost * x.Quantity).ToString(CultureInfo.InvariantCulture)
    };

    if (includeComponents)
      message.Components.AddRange(bikeBuild.BikeBuildComponents.Select(x => ToMessage(x, x.Component)));

    return message;
  }

  static BikeBuildComponentMessage ToMessage(Data.Entities.BikeBuildComponent bikeBuildComponent, Data.Entities.Component component) => new()
  {
    Id = bikeBuildComponent.Id,
    BikeBuildId = bikeBuildComponent.BikeBuildId,
    ComponentId = bikeBuildComponent.ComponentId,
    ComponentName = component.Name,
    Quantity = bikeBuildComponent.Quantity,
    Date = Timestamp.FromDateTimeOffset(bikeBuildComponent.Date),
    ComponentInformationJson = ComponentInformationSerializer.Serialize(component.Information)
  };
}
