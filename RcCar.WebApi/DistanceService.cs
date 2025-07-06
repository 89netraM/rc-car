using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Hcsr04;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnitsNet;

namespace RcCar.WebApi;

public class DistanceService(
    ILogger<DistanceService> logger,
    TimeProvider timeProvider,
    IOptions<DistanceOptions> options,
    IHubContext<ControllerHub> controllerHub
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enable)
        {
            return;
        }

        using var timer = new PeriodicTimer(options.Value.Interval, timeProvider);

        using var sensor = new Hcsr04(options.Value.TriggerPin, options.Value.EchoPin);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            logger.LogDebug("Reading distance...");
            Length? distance = null;
            if (sensor.TryGetDistance(out var d))
            {
                distance = d;
            }
            logger.LogDebug("Read distance {Distance}", distance);

            await controllerHub.Clients.All.SendAsync(
                nameof(IControllerClient.UpdateDistance),
                new IControllerClient.UpdateDistanceRequest(distance?.Centimeters),
                cancellationToken
            );
        }
    }
}

public class DistanceOptions
{
    public bool Enable { get; set; } = false;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(500);

    [Range(0, int.MaxValue)]
    public int TriggerPin { get; set; }

    [Range(0, int.MaxValue)]
    public int EchoPin { get; set; }
}

public static class DistanceServiceCollectionExtensions
{
    public static IServiceCollection AddDistanceService(this IServiceCollection services)
    {
        services.AddOptions<DistanceOptions>().BindConfiguration("Distance").ValidateOnStart();
        services.AddHostedService<DistanceService>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
