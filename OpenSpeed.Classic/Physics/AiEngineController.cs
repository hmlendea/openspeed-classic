using System;

namespace OpenSpeed.Classic.Physics
{
    public static class AiEngineController
    {
        public static void ApproachDesired(CarMemory car, int desired)
        {
            ArgumentNullException.ThrowIfNull(car);

            int current = car[0x2F0];

            if (desired > current)
            {
                car[0x2F0] = unchecked(current + X86Math.TruncatePowerOfTwo(desired - current, 3));
            }
            else if (desired < current)
            {
                car[0x2F0] = unchecked(current - X86Math.TruncatePowerOfTwo(current - desired, 3));
            }
        }

        public static int SelectTargetScalar(CarMemory car, bool alternateMode)
        {
            ArgumentNullException.ThrowIfNull(car);

            int result = alternateMode ? 0xF0000 : 0xC0000;

            if (car[0x558] > X86Math.One)
            {
                result = X86Math.MultiplyQ16(result, car[0x558]);
            }

            return result;
        }
    }
}