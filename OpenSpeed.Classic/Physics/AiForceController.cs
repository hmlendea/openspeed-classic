using System;

namespace OpenSpeed.Classic.Physics
{
    public static class AiForceController
    {
        public static void ApplyProportional(
            CarMemory car,
            int steeringGain,
            int steeringLimit,
            int lateralGainLimit,
            int lateralWindowScale,
            int steeringWindowScale,
            int longitudinalGain,
            int headingGain,
            int longitudinalLimit,
            int gain,
            FixedVector seed)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(seed);

            int speedMagnitude = X86Math.Abs(car[0x39C]);
            int steeringRaw = unchecked(
                X86Math.MultiplyQ16(0x80, steeringGain) * unchecked(-car[0x510]));
            steeringRaw = Clamp(steeringRaw, -steeringLimit, steeringLimit);

            int lateral = X86Math.MultiplyQ16(steeringRaw, 0xA0000);
            lateral = Clamp(lateral, -lateralGainLimit, lateralGainLimit);
            lateral = Clamp(lateral, -car[0x3A0], car[0x3A0]);

            if (unchecked(-0x1FFE0000) < car[0x39C] && car[0x39C] < 0x20000)
            {
                int lateralLimit = X86Math.Abs(X86Math.MultiplyQ16(car[0x3A0], lateralWindowScale));
                lateral = Clamp(lateral, -lateralLimit, lateralLimit);
                int steeringLimitInWindow = X86Math.Abs(X86Math.MultiplyQ16(car[0x3A0], steeringWindowScale));
                steeringRaw = Clamp(steeringRaw, -steeringLimitInWindow, steeringLimitInWindow);
            }

            int longitudinal = unchecked(-X86Math.MultiplyQ16(car[0x2C0] - steeringRaw, longitudinalGain));

            if (speedMagnitude > 0x120000)
            {
                longitudinal = X86Math.TruncatePowerOfTwo(longitudinal, 1);
            }

            longitudinal = Clamp(longitudinal, -longitudinalLimit, longitudinalLimit);
            int heading = unchecked(-X86Math.MultiplyQ16(car[0x2B0] - lateral, headingGain));
            heading = Clamp(heading, -car[0x550], car[0x550]);

            int controlledHeading = X86Math.MultiplyQ16(heading, gain);
            int controlledLateral = X86Math.MultiplyQ16(lateral, gain);
            int controlledLongitudinal = X86Math.MultiplyQ16(
                longitudinal,
                Math.Max(gain, 0x8000));

            car[0x324] = speedMagnitude;
            car[0x328] = speedMagnitude > 0xF0000 ? speedMagnitude : 0;
            car[0x2A8] = 0;
            car[0x2A4] = unchecked(seed.First + controlledLateral);
            car[0x2AC] = unchecked(seed.Second + controlledHeading);
            car[0x298] = 0;
            car[0x29C] = unchecked(seed.Third + controlledLongitudinal);
            car[0x2A0] = 0;
            car[0x2C8] = 0;
            car[0x2CC] = 0;
            car[0x2D0] = 0;
        }

        private static int Clamp(int value, int minimum, int maximum)
            => Math.Min(Math.Max(value, minimum), maximum);
    }
}