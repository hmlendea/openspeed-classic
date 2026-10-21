using System;

namespace OpenSpeed.Classic.Physics
{
    public static class NativeContactGravity
    {
        private static int RouteIndexOffset => 0x14;

        private static int MotionScaleOffset => 0x94;

        private static int VerticalVelocityOffset => 0xAC;

        private static int ContactLimitOffset => 0x150;

        private static int RoleFlagsOffset => 0x1F4;

        private static int TrackScaledRoleMask => 0x04;

        private static int ScaledTrackIdentifier => 7;

        private static int ScaledRouteStart => 0x144;

        private static int ScaledRouteEnd => 0x230;

        private static int TrackGravityScale => 0x30000;

        private static int GravityStep => 0x7599;

        private static int ContactLimit => 0x640000;

        public static int Apply(CarMemory car, int trackIdentifier, int scheduleScalar)
        {
            ArgumentNullException.ThrowIfNull(car);
            int scaledAngle = car[MotionScaleOffset];

            if ((car[RoleFlagsOffset] & TrackScaledRoleMask) != 0)
            {
                scaledAngle = X86Math.MultiplyQ16(
                    CalculateTrackScale(trackIdentifier, car[RouteIndexOffset]),
                    scaledAngle);
            }

            car[ContactLimitOffset] = ContactLimit;
            int scheduledGravity = unchecked(scheduleScalar * GravityStep);
            int gravity = X86Math.MultiplyQ16(scheduledGravity, scaledAngle);
            car[VerticalVelocityOffset] = unchecked(car[VerticalVelocityOffset] - gravity);

            return scaledAngle;
        }

        private static int CalculateTrackScale(int trackIdentifier, int routeIndex)
        {
            if (trackIdentifier == ScaledTrackIdentifier &&
                routeIndex > ScaledRouteStart &&
                routeIndex < ScaledRouteEnd)
            {
                return TrackGravityScale;
            }

            return X86Math.One;
        }
    }
}