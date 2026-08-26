using System.Globalization;
using BikeBuilder.API.Protos;
using BikeBuilder.Contracts.Events;
using Grpc.Core;

namespace BikeBuilder.API.Services;

public class ComponentGrpcService(BikeBuilderDbContext db, ComponentImageStorageService storage, IEventPublisher eventPublisher) : ComponentService.ComponentServiceBase
{
    public override async Task<ListComponentsResponse> ListComponents(ListComponentsRequest request, ServerCallContext context)
    {
        var components = await db.Components.Include(c => c.Image).AsNoTracking().ToListAsync(context.CancellationToken);

        var response = new ListComponentsResponse();
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
            Description = request.Description
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
        {
            await storage.DeleteAsync(component.Image.BlobName, context.CancellationToken);
        }

        return new DeleteComponentResponse { Success = true };
    }

    private static ComponentMessage ToMessage(Data.Entities.Component component) => new()
    {
        Id = component.Id,
        Name = component.Name,
        Cost = component.Cost.ToString(CultureInfo.InvariantCulture),
        Description = component.Description,
        HasImage = component.Image is not null,
        ImageVersion = component.Image?.UploadedAt.UtcTicks ?? 0
    };

    private static decimal ParseCost(string cost)
    {
        if (!decimal.TryParse(cost, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid cost value: '{cost}'."));
        }

        return value;
    }
}
