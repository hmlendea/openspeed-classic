using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates <c>sub_465F60</c> (with its <c>sub_466200</c> helper): the route-bound lateral
    /// target pass of the alternate contact pipeline. It derives the lateral target <c>+0x394</c>
    /// from the smoothed steering, a race-direction bit, and three adjacent route records, then
    /// writes the longitudinal contact force <c>+0x30C</c> and clears <c>+0x368</c>.
    /// </summary>
    /// <remarks>
    /// The source delegates the wide-deviation and negative-projection cases to
    /// <c>sub_466280</c>; that unresolved route-geometry path is supplied through
    /// <paramref name="wideDeviationQuery"/>. The race-direction bit read from
    /// <c>dword_512228</c> is supplied explicitly, matching the existing managed boundary for
    /// race-generated globals.
    /// </remarks>
    public static class RouteLateralTarget
    {
        private static int ContactGateOffset => 0x288;

        private static int LateralPositionOffset => 0x3AC;

        private static int LateralTargetOffset => 0x394;

        private static int SteeringOffset => 0x2E4;

        private static int PreviousComparisonOffset => 0x204;

        private static int ProjectedNormalOffset => 0x2B8;

        private static int ForceOutputOffset => 0x30C;

        private static int RouteIndexOffset => 0x14;

        private static int DeviationLimit => 0x38000;

        private static int TurnHalfThreshold => 0x1E;

        private static int HeadingHalfThreshold => 0x64;

        private static int SteeringDivisor => 0x7C;

        private static int ProjectionScale => 0x1999;

        private static int ForceClamp => 0x7F;

        /// <summary>
        /// Runs the route-bound lateral target pass. The source is entered only when the car's
        /// <c>+0x288</c> gate is zero.
        /// </summary>
        /// <param name="car">The car state.</param>
        /// <param name="descriptor">The car descriptor, read at <c>+0x12C</c>.</param>
        /// <param name="route">The decoded XBID 15 route.</param>
        /// <param name="trackMode">The <c>sub_43D830</c> track-mode value used by <c>sub_466200</c>.</param>
        /// <param name="raceDirection">The race-direction bit normally held in <c>dword_512228</c>.</param>
        /// <param name="wideDeviationQuery">
        /// The unresolved far-deviation path. Called instead of the source's <c>sub_466280</c>
        /// when the deviation exceeds <c>0x38000</c> or the projected normal is negative.
        /// </param>
        public static void Update(
            CarMemory car,
            CarSpecifications descriptor,
            PhysicsRoute route,
            int trackMode,
            int raceDirection,
            Action<CarMemory> wideDeviationQuery)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(wideDeviationQuery);

            int baseFlag = car[ContactGateOffset] > 0 ? 1 : 0;
            int directionFlag = (baseFlag ^ raceDirection) == 0 ? 1 : 0;
            int steering = car[SteeringOffset];

            if (directionFlag == 0)
            {
                steering = X86Math.Negate(steering);
            }

            int scaledSteering = unchecked(steering * 5);
            scaledSteering = unchecked(scaledSteering << 0x11);
            int quotient = X86Math.DivideSigned(scaledSteering, SteeringDivisor);
            int routeOffset = Sub466200(car, route, trackMode);
            int combined = unchecked(routeOffset + quotient);
            car[LateralTargetOffset] = combined;
            int difference = unchecked(car[LateralPositionOffset] - combined);
            int truncatedDifference = X86Math.TruncatePowerOfTwo(difference, 16);

            if (difference <= 0)
            {
                difference = X86Math.Negate(difference);
            }

            if (difference > DeviationLimit || car[ProjectedNormalOffset] < 0)
            {
                wideDeviationQuery(car);

                return;
            }

            int currentIndex = car[RouteIndexOffset];
            int count = route.Count;
            int primaryIndex;
            int secondaryIndex;

            if (directionFlag == 0)
            {
                primaryIndex = currentIndex - 1;

                if (primaryIndex < 0)
                {
                    primaryIndex = unchecked(primaryIndex + count);
                }

                secondaryIndex = currentIndex - 2;

                if (secondaryIndex < 0)
                {
                    secondaryIndex = unchecked(secondaryIndex + count);
                }
            }
            else
            {
                primaryIndex = currentIndex + 1;

                if (primaryIndex >= count)
                {
                    primaryIndex = unchecked(primaryIndex - count);
                }

                secondaryIndex = currentIndex + 2;

                if (secondaryIndex >= count)
                {
                    secondaryIndex = unchecked(secondaryIndex - count);
                }
            }

            int firstAngle = PhysicsAngles.RatioAngle(
                unchecked(route.ReadDword(primaryIndex, 0) - route.ReadDword(currentIndex, 0)),
                unchecked(route.ReadDword(primaryIndex, 8) - route.ReadDword(currentIndex, 8)));
            int secondAngle = PhysicsAngles.RatioAngle(
                unchecked(route.ReadDword(secondaryIndex, 0) - route.ReadDword(primaryIndex, 0)),
                unchecked(route.ReadDword(secondaryIndex, 8) - route.ReadDword(primaryIndex, 8)));
            int turn = PhysicsHelpers.AngleDifference(secondAngle, firstAngle);

            if (turn <= 0)
            {
                turn = X86Math.Negate(turn);
            }

            if (turn > TurnHalfThreshold)
            {
                turn = X86Math.TruncatePowerOfTwo(turn, 1);
            }

            int headingDelta = PhysicsHelpers.AngleDifference(firstAngle, car[PreviousComparisonOffset]);

            if (headingDelta <= 0)
            {
                headingDelta = X86Math.Negate(headingDelta);
            }

            if (headingDelta > HeadingHalfThreshold)
            {
                headingDelta = X86Math.TruncatePowerOfTwo(headingDelta, 1);
            }

            int steepness = descriptor[0x12C];
            int projected = car[ProjectedNormalOffset];

            if (projected < 0)
            {
                projected = X86Math.Negate(projected);
            }

            int scale = X86Math.TruncatePowerOfTwo(
                X86Math.MultiplyQ16(X86Math.MultiplyQ16(projected, ProjectionScale), steepness), 7);

            if (directionFlag != 0)
            {
                truncatedDifference = X86Math.Negate(truncatedDifference);
            }

            int forceScale = unchecked(turn + headingDelta + truncatedDifference);

            if (forceScale > ForceClamp)
            {
                forceScale = ForceClamp;
            }
            else if (forceScale < unchecked(-ForceClamp))
            {
                forceScale = unchecked(-ForceClamp);
            }

            car[0x368] = 0;
            car[ForceOutputOffset] = unchecked(X86Math.Multiply(scale, forceScale));
        }

        /// <summary>
        /// Translates <c>sub_466200</c>: the adjacent-record lateral offset. The record is the
        /// car's current route index; the byte at record offset <c>0x1F</c> supplies a nibble
        /// combined with the byte at <c>0x20</c> or <c>0x21</c> according to
        /// <param name="trackMode"/>. The routine returns the truncated scaled nibble product
        /// from either branch; its sole caller only uses the resulting <c>eax</c> as the
        /// <c>sub_466280</c> argument, so the value is returned directly.
        /// </summary>
        public static int Sub466200(CarMemory car, PhysicsRoute route, int trackMode)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(route);

            int index = car[RouteIndexOffset];

            if (trackMode == 1)
            {
                int scaled = route.ReadByte(index, 0x21) << 15;
                int nibble = route.ReadByte(index, 0x1F) & 0x0F;

                return X86Math.TruncatePowerOfTwo(unchecked(nibble * scaled), 1);
            }

            int shifted = route.ReadByte(index, 0x20) << 15;
            int highNibble = (route.ReadByte(index, 0x1F) & 0xFF) >> 4 & 0x0F;

            return X86Math.TruncatePowerOfTwo(unchecked(highNibble * X86Math.Negate(shifted)), 1);
        }
    }
}
