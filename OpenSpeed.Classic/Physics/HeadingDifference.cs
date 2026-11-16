using System;

namespace OpenSpeed.Classic.Physics
{
    public static class HeadingDifference
    {
        public static int Calculate(CarMemory car, int routeCount, Func<int, int> angleLookup)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(angleLookup);

            int delta = car[0x148] - car[0x204];

            if (delta < 0)
            {
                delta = unchecked(-delta);
            }

            if (delta > 0x200)
            {
                delta = unchecked(0x400 - delta);
            }

            int adjusted = car[0x14];

            if (delta > 0x100)
            {
                adjusted = unchecked(adjusted - 0x0F);

                if (adjusted < 0)
                {
                    adjusted = unchecked(adjusted + routeCount);
                }
            }
            else
            {
                adjusted = unchecked(adjusted + 0x0F);

                if (adjusted >= routeCount)
                {
                    adjusted = unchecked(adjusted - routeCount);
                }
            }

            int result = unchecked(angleLookup(adjusted) - car[0x148]);

            if (result > 0x200)
            {
                result = unchecked(result - 0x400);
            }

            if (result < -0x200)
            {
                result = unchecked(result + 0x400);
            }

            return result;
        }
    }
}