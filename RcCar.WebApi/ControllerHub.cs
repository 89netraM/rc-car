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

    public record SetAccelerationRequest(double Acceleration);

    public record SetSteeringRequest(double Steering);
}

[JsonSerializable(typeof(ControllerHub.SetAccelerationRequest))]
[JsonSerializable(typeof(ControllerHub.SetSteeringRequest))]
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
