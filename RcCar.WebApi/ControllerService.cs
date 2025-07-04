using System;
using System.ComponentModel.DataAnnotations;
using System.Device.Gpio;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RcCar.WebApi;

public class ControllerService(
    ILogger<ControllerService> logger,
    IOptions<ControllerOptions> options,
    TimeProvider timeProvider,
    ControllerSettings settings
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Configuring GPIO for car control...");
        using var controller = new GpioController();
        using var acceleration = controller.OpenPair(
            options.Value.AccelerationPins.Positive,
            options.Value.AccelerationPins.Negative
        );
        using var steering = controller.OpenPair(
            options.Value.SteeringPins.Positive,
            options.Value.SteeringPins.Negative
        );
        logger.LogDebug("GPIO configured");

        using var timer = new PeriodicTimer(options.Value.UpdateInterval, timeProvider);

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

            UpdatePins(acceleration, settings.Acceleration);
            UpdatePins(steering, settings.Steering);
        }
    }

    private void UpdatePins(
        GpioPinPair pins,
        double value,
        [CallerArgumentExpression(nameof(pins))] string pinsName = ""
    )
    {
        const double Zero = 0.005d;

        if (value > Zero)
        {
            var didChange = pins.ActivatePositive();
            if (didChange)
            {
                logger.LogDebug("Activating positive pin for {Pins}", pinsName);
            }
        }
        else if (value < -Zero)
        {
            var didChange = pins.ActivateNegative();
            if (didChange)
            {
                logger.LogDebug("Activating negative pin for {Pins}", pinsName);
            }
        }
        else
        {
            var didChange = pins.Deactivate();
            if (didChange)
            {
                logger.LogDebug("Deactivating pins for {Pins}", pinsName);
            }
        }
    }
}

public class ControllerSettings
{
    private double acceleration = 0.0d;
    public double Acceleration
    {
        get => acceleration;
        set => acceleration = double.Clamp(value, -1.0d, 1.0d);
    }

    private double steering = 0.0d;
    public double Steering
    {
        get => steering;
        set => steering = double.Clamp(value, -1.0d, 1.0d);
    }
}

public class ControllerOptions
{
    [Required, ValidateObjectMembers]
    public required PinPairOptions AccelerationPins { get; set; }

    [Required, ValidateObjectMembers]
    public required PinPairOptions SteeringPins { get; set; }

    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMilliseconds(20);

    public class PinPairOptions
    {
        [Required, Range(0, int.MaxValue)]
        public int Positive { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int Negative { get; set; }
    }
}

[OptionsValidator]
public partial class ControllerOptionsValidator : IValidateOptions<ControllerOptions>;

public static class ControllerServiceCollectionExtensions
{
    public static IServiceCollection AddControllerService(this IServiceCollection services)
    {
        services.AddOptions<ControllerOptions>().BindConfiguration("Controller").ValidateOnStart();
        services.AddTransient<IValidateOptions<ControllerOptions>, ControllerOptionsValidator>();
        services.AddSingleton<ControllerSettings>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<ControllerService>();
        return services;
    }
}
