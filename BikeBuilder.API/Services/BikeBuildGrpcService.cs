using System.Globalization;
using BikeBuilder.API.Protos;
using BikeBuilder.Contracts.Events;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace BikeBuilder.API.Services;

public class BikeBuildGrpcService(BikeBuilderDbContext db, IEventPublisher eventPublisher) : BikeBuildService.BikeBuildServiceBase
{
  public override async Task<ListBikeBuildsResponse> ListBikeBuilds(ListBikeBuildsRequest request, ServerCallContext context)
  {
    var bikeBuilds = await db.BikeBuilds
        .Include(b => b.BikeBuildComponents)
        .ThenInclude(x => x.Component)
        .AsNoTracking()
        .ToListAsync(context.CancellationToken);

    var response = new ListBikeBuildsResponse();
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

    return ToMessage(bikeBuildComponent, component.Name);
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

    return ToMessage(bikeBuildComponent, component.Name);
  }

  public override async Task<RemoveBikeBuildComponentResponse> RemoveBikeBuildComponent(RemoveBikeBuildComponentRequest request, ServerCallContext context)
  {
    var bikeBuildComponent = await db.BikeBuildComponents.FirstOrDefaultAsync(x => x.Id == request.Id, context.CancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"BikeBuildComponent {request.Id} not found."));

    db.BikeBuildComponents.Remove(bikeBuildComponent);
    await db.SaveChangesAsync(context.CancellationToken);

    return new RemoveBikeBuildComponentResponse { Success = true };
  }

  private async Task<Data.Entities.BikeBuild> LoadBikeBuildWithComponents(int id, CancellationToken cancellationToken)
  {
    return await db.BikeBuilds
        .Include(b => b.BikeBuildComponents)
        .ThenInclude(x => x.Component)
        .AsNoTracking()
        .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, $"BikeBuild {id} not found."));
  }

  private static BikeBuildMessage ToMessage(Data.Entities.BikeBuild bikeBuild, bool includeComponents)
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
    {
      message.Components.AddRange(bikeBuild.BikeBuildComponents.Select(x => ToMessage(x, x.Component.Name)));
    }

    return message;
  }

  private static BikeBuildComponentMessage ToMessage(Data.Entities.BikeBuildComponent bikeBuildComponent, string componentName) => new()
  {
    Id = bikeBuildComponent.Id,
    BikeBuildId = bikeBuildComponent.BikeBuildId,
    ComponentId = bikeBuildComponent.ComponentId,
    ComponentName = componentName,
    Quantity = bikeBuildComponent.Quantity,
    Date = Timestamp.FromDateTimeOffset(bikeBuildComponent.Date)
  };
}
