using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RcCar.WebApi;

public class ControllerHub(ILogger<ControllerHub> logger, ControllerService controller) : Hub
{
    public void SetAcceleration(SetAccelerationRequest request)
    {
        logger.LogDebug("Received acceleration {Acceleration} from client", request.Acceleration);
        controller.Acceleration = request.Acceleration;
    }

    public void SetSteering(SetSteeringRequest request)
    {
        logger.LogDebug("Received steering {Steering} from client", request.Steering);
        controller.Steering = request.Steering;
    }

    public void SetHorn(SetHornRequest request)
    {
        logger.LogDebug("Received horn {Horn} from client", request.Active);
        controller.Horn = request.Active;
    }

    public record SetAccelerationRequest(double Acceleration);

    public record SetSteeringRequest(double Steering);

    public record SetHornRequest(bool Active);
}

[JsonSerializable(typeof(ControllerHub.SetAccelerationRequest))]
[JsonSerializable(typeof(ControllerHub.SetSteeringRequest))]
[JsonSerializable(typeof(ControllerHub.SetHornRequest))]
public partial class ControllerHubJsonSerializerContext : JsonSerializerContext;

public static class ControllerHubServiceCollectionExtensions
{
    public static IServiceCollection AddControllerHub(this IServiceCollection services)
    {
        services
            .AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.TypeInfoResolverChain.Add(ControllerHubJsonSerializerContext.Default);
            });
        return services;
    }
}
