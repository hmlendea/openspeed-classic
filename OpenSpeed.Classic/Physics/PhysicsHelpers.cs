using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Small exact helpers shared by the drivetrain and contact paths.
    /// </summary>
    public static class PhysicsHelpers
    {
        private static int TorqueCurveLength => 0x28;

        /// <summary>
        /// Translates <c>sub_46B980</c>: the torque-curve sampler. The supplied index is clamped
        /// to <c>[0, 0x28]</c> and used to read <c>descriptor[0x4C + index * 4]</c>. The clamp is
        /// inclusive at the upper bound and uses the signed compare order of the source, so a
        /// negative index is clamped to zero and an oversized index to <c>0x28</c>.
        /// </summary>
        public static int SampleTorqueCurve(CarSpecifications descriptor, int index)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            int clamped = index;

            if (clamped > TorqueCurveLength)
            {
                clamped = TorqueCurveLength;
            }
            else if (clamped <= 0)
            {
                clamped = 0;
            }

            return descriptor[0x4C + clamped * sizeof(int)];
        }

        /// <summary>
        /// Translates <c>sub_460930</c>: the route-index contact scale used by the track-contact
        /// entry path. Returns <c>0x30000</c> only when the race type is <c>7</c> and the signed
        /// route index is strictly inside <c>(0x144, 0x230)</c>; otherwise <c>0x10000</c>.
        /// </summary>
        public static int RouteContactScale(int raceType, int routeIndex)
        {
            int scale = X86Math.One;

            if (raceType == 7 && routeIndex > 0x144 && routeIndex < 0x230)
            {
                scale = 0x30000;
            }

            return scale;
        }

        /// <summary>
        /// Translates <c>sub_45E710</c>: the wrapped signed angle difference. Both inputs are
        /// signed cyclic angles; the result is normalised into <c>[-0x200, 0x200]</c>. The branch
        /// structure uses the raw signed operands, so a negative second input is handled by a
        /// distinct path rather than by an absolute value.
        /// </summary>
        public static int AngleDifference(int first, int second)
        {
            int difference = unchecked(first - second);

            if (first >= 0)
            {
                if (second >= 0)
                {
                    return difference;
                }

                if (difference > 0x200)
                {
                    return unchecked(difference - 0x400);
                }

                return difference;
            }

            if (second < 0)
            {
                return difference;
            }

            int magnitude = unchecked(second - first);

            if (magnitude > 0x200)
            {
                return unchecked(0x400 - magnitude);
            }

            return unchecked(-magnitude);
        }

        /// <summary>
        /// Translates <c>sub_464700</c>: the <c>+0x31C</c> tally helper. A positive counter is
        /// incremented; once it exceeds <c>0x80</c> the byte at <c>+0x318</c> is set and the
        /// counter resets. A non-positive counter is left untouched.
        /// </summary>
        /// <returns>True when the byte at <c>+0x318</c> was set on this call.</returns>
        public static bool UpdateTally(CarMemory car)
        {
            ArgumentNullException.ThrowIfNull(car);
            int counter = car[0x31C];

            if (counter <= 0)
            {
                return false;
            }

            counter = unchecked(counter + 1);

            if (counter > 0x80)
            {
                car.WriteByte(0x318, 1);
                car[0x31C] = 0;

                return true;
            }

            car[0x31C] = counter;

            return false;
        }
    }
}