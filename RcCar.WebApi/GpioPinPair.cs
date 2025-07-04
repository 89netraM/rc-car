using System;
using System.Device.Gpio;

namespace RcCar.WebApi;

public sealed class GpioPinPair(GpioPin positive, GpioPin negative) : IDisposable
{
    private PairState state = PairState.Neutral;

    public bool ActivatePositive()
    {
        if (state is PairState.Positive)
        {
            return false;
        }

        negative.Write(PinValue.Low);
        positive.Write(PinValue.High);

        state = PairState.Positive;
        return true;
    }

    public bool Deactivate()
    {
        if (state is PairState.Neutral)
        {
            return false;
        }

        negative.Write(PinValue.Low);
        positive.Write(PinValue.Low);

        state = PairState.Neutral;
        return true;
    }

    public bool ActivateNegative()
    {
        if (state is PairState.Negative)
        {
            return false;
        }

        positive.Write(PinValue.Low);
        negative.Write(PinValue.High);

        state = PairState.Negative;
        return true;
    }

    public void Dispose()
    {
        positive.Dispose();
        negative.Dispose();
    }

    private enum PairState
    {
        Negative = -1,
        Neutral = 0,
        Positive = 1,
    }
}

public static class GpioPinPairGpioControllerExtensions
{
    public static GpioPinPair OpenPair(this GpioController controller, int positivePinNumber, int negativePinNumber)
    {
        var positivePin = controller.OpenPin(positivePinNumber, PinMode.Output, PinValue.Low);
        var negativePin = controller.OpenPin(negativePinNumber, PinMode.Output, PinValue.Low);
        return new(positivePin, negativePin);
    }
}
