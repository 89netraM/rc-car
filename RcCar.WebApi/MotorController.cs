using System;
using System.Device.Gpio;
using System.Device.Pwm;

namespace RcCar.WebApi;

public sealed class MotorController(PwmChannel pwm, GpioPin positive, GpioPin negative) : IDisposable
{
    private double value = 0.0d;
    public double Value
    {
        get => value;
        set => SetValue(value);
    }

    private void SetValue(double value)
    {
        switch (value)
        {
            case < 0.0d:
                positive.Write(PinValue.Low);
                negative.Write(PinValue.High);
                break;
            case 0.0d:
                positive.Write(PinValue.Low);
                negative.Write(PinValue.Low);
                break;
            case > 0.0d:
                negative.Write(PinValue.Low);
                positive.Write(PinValue.High);
                break;
        }

        pwm.DutyCycle = double.Abs(value);

        this.value = value;
    }

    public void Dispose()
    {
        pwm.Dispose();
        positive.Dispose();
        negative.Dispose();
    }
}

public static class MotorControllerGpioControllerExtensions
{
    public static MotorController OpenMotorController(
        this GpioController gpioController,
        int chip,
        int channel,
        int frequency,
        int positive,
        int negative
    )
    {
        var pwm = PwmChannel.Create(chip, channel, frequency, 0.0d);
        pwm.Start();
        return new(
            pwm,
            gpioController.OpenPin(positive, PinMode.Output, PinValue.Low),
            gpioController.OpenPin(negative, PinMode.Output, PinValue.Low)
        );
    }
}
