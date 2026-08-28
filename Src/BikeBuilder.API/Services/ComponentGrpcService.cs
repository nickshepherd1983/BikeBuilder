namespace BikeBuilder.API.Services;

public class ComponentGrpcService(BikeBuilderDbContext db, ComponentImageStorageService storage, IEventPublisher eventPublisher) : ComponentService.ComponentServiceBase
{
  public override async Task<ListComponentsResponse> ListComponents(ListComponentsRequest request, ServerCallContext context)
  {
    IQueryable<Data.Entities.Component> query = db.Components.Include(c => c.Image).AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Search))
      query = query.Where(c => c.Name.Contains(request.Search) || c.Sku.Contains(request.Search));

    var totalCount = await query.CountAsync(context.CancellationToken);

    query = (request.SortField, request.SortDescending) switch
    {
      (ComponentSortField.Name, true) => query.OrderByDescending(c => c.Name).ThenByDescending(c => c.Id),
      (ComponentSortField.Cost, false) => query.OrderBy(c => c.Cost).ThenBy(c => c.Id),
      (ComponentSortField.Cost, true) => query.OrderByDescending(c => c.Cost).ThenByDescending(c => c.Id),
      (ComponentSortField.Sku, false) => query.OrderBy(c => c.Sku).ThenBy(c => c.Id),
      (ComponentSortField.Sku, true) => query.OrderByDescending(c => c.Sku).ThenByDescending(c => c.Id),
      // Manufacturer is stored as a string (HasConversion<string>), so this sorts alphabetically
      // by the stored name (Hope < Other < Shimano < Sram), not by enum value.
      (ComponentSortField.Manufacturer, false) => query.OrderBy(c => c.Manufacturer).ThenBy(c => c.Id),
      (ComponentSortField.Manufacturer, true) => query.OrderByDescending(c => c.Manufacturer).ThenByDescending(c => c.Id),
      _ => query.OrderBy(c => c.Name).ThenBy(c => c.Id)
    };

    if (request.Page > 0)
    {
      var pageSize = Math.Clamp(request.Limit <= 0 ? 20 : request.Limit, 1, 100);
      query = query.Skip((request.Page - 1) * pageSize).Take(pageSize);
    }
    else if (request.Limit > 0)
    {
      query = query.Take(request.Limit);
    }

    var components = await query.ToListAsync(context.CancellationToken);

    var response = new ListComponentsResponse { TotalCount = totalCount };
    response.Components.AddRange(components.Select(ToMessage));
    return response;
  }

  public override async Task<ComponentMessage> GetComponent(GetComponentRequest request, ServerCallContext context)
  {
    var component = await db.Components.Include(c => c.Image).AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"Component {request.Id} not found."));

    return ToMessage(component);
  }

  public override async Task<ComponentMessage> CreateComponent(CreateComponentRequest request, ServerCallContext context)
  {
    var component = new Data.Entities.Component
    {
      Name = request.Name,
      Cost = ParseCost(request.Cost),
      Description = request.Description,
      Sku = request.Sku,
      Manufacturer = (Data.Entities.Manufacturer)(int)request.Manufacturer,
      Information = ParseComponentInformation(request.ComponentInformationJson)
    };

    db.Components.Add(component);
    await db.SaveChangesAsync(context.CancellationToken);

    await eventPublisher.PublishAsync(ServiceBusMessageTypes.ComponentCreated,
        new ComponentCreatedEvent
        {
          Id = component.Id,
          Name = component.Name,
          Cost = component.Cost,
          CreatedAt = DateTimeOffset.UtcNow
        },
        context.CancellationToken);

    return ToMessage(component);
  }

  public override async Task<ComponentMessage> UpdateComponent(UpdateComponentRequest request, ServerCallContext context)
  {
    var component = await db.Components.Include(c => c.Image).FirstOrDefaultAsync(c => c.Id == request.Id, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"Component {request.Id} not found."));

    component.Name = request.Name;
    component.Cost = ParseCost(request.Cost);
    component.Description = request.Description;
    component.Sku = request.Sku;
    component.Manufacturer = (Data.Entities.Manufacturer)(int)request.Manufacturer;
    component.Information = ParseComponentInformation(request.ComponentInformationJson);

    await db.SaveChangesAsync(context.CancellationToken);

    return ToMessage(component);
  }

  public override async Task<DeleteComponentResponse> DeleteComponent(DeleteComponentRequest request, ServerCallContext context)
  {
    var component = await db.Components.Include(c => c.Image).FirstOrDefaultAsync(c => c.Id == request.Id, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"Component {request.Id} not found."));

    db.Components.Remove(component);

    try
    {
      await db.SaveChangesAsync(context.CancellationToken);
    }
    catch (DbUpdateException ex)
    {
      throw new RpcException(new Status(StatusCode.FailedPrecondition,
          "This component is still used by one or more bike builds and cannot be deleted."), ex.Message);
    }

    if (component.Image is not null)
      await storage.DeleteAsync(component.Image.BlobName, context.CancellationToken);

    return new DeleteComponentResponse { Success = true };
  }

  static ComponentMessage ToMessage(Data.Entities.Component component) => new()
  {
    Id = component.Id,
    Name = component.Name,
    Cost = component.Cost.ToString(CultureInfo.InvariantCulture),
    Description = component.Description,
    Sku = component.Sku,
    Manufacturer = (Protos.Manufacturer)(int)component.Manufacturer,
    HasImage = component.Image is not null,
    ImageVersion = component.Image?.UploadedAt.UtcTicks ?? 0,
    ComponentInformationJson = ComponentInformationSerializer.Serialize(component.Information)
  };

  static decimal ParseCost(string cost)
  {
    if (!decimal.TryParse(cost, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
    {
      throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid cost value: '{cost}'."));
    }

    return value;
  }

  static ComponentInformation? ParseComponentInformation(string json)
  {
    if (string.IsNullOrWhiteSpace(json))
      return null;

    try
    {
      return ComponentInformationSerializer.Deserialize(json);
    }
    catch (Exception ex) when (ex is JsonException or NotSupportedException)
    {
      throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid component information: {ex.Message}"));
    }
  }
}
