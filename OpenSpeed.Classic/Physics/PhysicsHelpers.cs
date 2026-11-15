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
    }
}