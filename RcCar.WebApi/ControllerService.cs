using System;
using System.ComponentModel.DataAnnotations;
using System.Device.Gpio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace RcCar.WebApi;

public sealed class ControllerService : IDisposable
{
    private readonly GpioController gpioController;

    private readonly MotorController acceleration;
    public double Acceleration
    {
        get => acceleration.Value;
        set => acceleration.Value = double.Clamp(value, -1.0d, 1.0d);
    }

    private readonly MotorController steering;
    public double Steering
    {
        get => steering.Value;
        set => steering.Value = double.Clamp(value, -1.0d, 1.0d);
    }

    public ControllerService(IOptions<ControllerOptions> options)
    {
        gpioController = new GpioController();
        acceleration = gpioController.OpenMotorController(
            options.Value.Acceleration.Chip,
            options.Value.Acceleration.Channel,
            options.Value.Frequency,
            options.Value.Acceleration.PositivePin,
            options.Value.Acceleration.NegativePin
        );
        steering = gpioController.OpenMotorController(
            options.Value.Steering.Chip,
            options.Value.Steering.Channel,
            options.Value.Frequency,
            options.Value.Steering.PositivePin,
            options.Value.Steering.NegativePin
        );
    }

    public void Dispose()
    {
        acceleration.Dispose();
        steering.Dispose();
        gpioController.Dispose();
    }
}

public class ControllerOptions
{
    [Required, ValidateObjectMembers]
    public required PinPairOptions Acceleration { get; set; }

    [Required, ValidateObjectMembers]
    public required PinPairOptions Steering { get; set; }

    [Range(1, int.MaxValue)]
    public int Frequency { get; set; } = 100;

    public class PinPairOptions
    {
        [Required, Range(0, int.MaxValue)]
        public int Chip { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int Channel { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int PositivePin { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int NegativePin { get; set; }
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
        services.AddSingleton<ControllerService>();
        return services;
    }
}
