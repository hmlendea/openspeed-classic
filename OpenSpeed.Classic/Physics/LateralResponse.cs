using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates <c>sub_453670</c>: the lateral response applied to the local third-axis force
    /// <c>+0x2AC</c>. The routine forms the basis projection of the constant <c>0xFFF60000</c>
    /// through the primary basis row <c>+0xDC..+0xE4</c>, optionally divides it by eight when the
    /// caller passes a nonzero mask, scales it by <c>+0x94</c>, multiplies it by the current local
    /// force, and then adds either a quarter of the scaled value or the whole scaled value to
    /// <c>+0x2AC</c>.
    /// </summary>
    public static class LateralResponse
    {
        private static int GravityInput => unchecked((int)0xFFF60000);

        /// <summary>
        /// Applies the lateral response.
        /// </summary>
        /// <param name="car">The car state.</param>
        /// <param name="context">The per-update physics context.</param>
        /// <param name="isAlternate">
        /// The caller's <c>edx</c> argument. The source tests it for zero only, so any nonzero
        /// value selects the division-by-eight branch.
        /// </param>
        public static void Apply(CarMemory car, PhysicsContext context, bool isAlternate)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(context);

            if ((car.ReadByte(0x1F4) & 0x10) != 0 ||
                (car.ReadByte(0x1F4) & 0x40) != 0 ||
                car.ReadByte(0x2D8) < 0x0A)
            {
                return;
            }

            // The projection multiplies all three basis components, matching the source's three
            // imul sequences; the zero components still participate in the accumulation order.
            int projection = X86Math.MultiplyQ16(car[0xDC], 0);
            projection = unchecked(projection + X86Math.MultiplyQ16(car[0xE0], GravityInput));
            projection = unchecked(projection + X86Math.MultiplyQ16(car[0xE4], 0));

            if (isAlternate)
            {
                projection = X86Math.TruncatePowerOfTwo(projection, 3);
            }

            int scale = X86Math.MultiplyQ16(projection, car[0x94]);
            int response = X86Math.MultiplyQ16(scale, car[0x2AC]);
            int bound = PhysicsTables.LateralResponseBound;

            // The quarter branch requires the response to be inside the signed bounds and the
            // mode to be nonzero; the source falls through to the full addition otherwise.
            bool withinBounds = context.Mode != 0 &&
                response <= bound && response >= unchecked(-bound);

            if (withinBounds)
            {
                car[0x2AC] = unchecked(car[0x2AC] + X86Math.TruncatePowerOfTwo(scale, 2));
            }
            else
            {
                car[0x2AC] = unchecked(car[0x2AC] + scale);
            }
        }
    }
}