using System;
using System.Collections.Generic;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates <c>sub_467A90</c>, the optional nearest-opponent collision selection that the
    /// dispatcher runs before its projection branch. The scan is gated by the car's recovery
    /// timer, its race/progression selector, the global replay flag, the collision role bit, and
    /// the contact-suppression timer. Each candidate's normalised position delta is projected
    /// through the car's primary-basis row 2; only a positive projection above
    /// <c>0xCCCC</c> and a resulting distance strictly inside <c>0x280000</c> can win. The
    /// selected candidate is handed to <c>sub_4540A0</c>.
    /// </summary>
    public static class OpponentCollisionSelection
    {
        private static int RecoverySuppressionOffset => 0x17C;

        private static int CountdownOffset => 0x2E8;

        private static int RaceSelectorOffset => 0x280;

        private static int RoleFlagsOffset => 0x1F4;

        private static int ActiveBodyOffset => 0x8D;

        private static int CollisionCountOffset => 0x14C;

        private static int PositionOffset => 0x9C;

        private static int BasisRow2Offset => 0xDC;

        private static int PlayerRoleMask => 0x04;

        private static int MinimumProjection => 0xCCCC;

        private static int MaximumDistance => 0x280000;

        private static int InitialBestDistance => 0x280000;

        /// <summary>
        /// Returns the selected opponent, or null when every gate rejects the scan or no
        /// candidate satisfies the projection and distance tests. The caller owns the
        /// <c>sub_4540A0</c> invocation.
        /// </summary>
        public static CarMemory? Select(
            CarMemory car,
            IReadOnlyList<CarMemory> candidates,
            int replayOrRaceFlag,
            int recoveryTimerOffset)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(candidates);

            if (car[CountdownOffset] != 0 ||
                car[RaceSelectorOffset] == 2 ||
                replayOrRaceFlag == 0 ||
                (car.ReadByte(RoleFlagsOffset) & PlayerRoleMask) == 0 ||
                car[RecoverySuppressionOffset] != 0)
            {
                return null;
            }

            CarMemory? best = null;
            int bestDistance = InitialBestDistance;

            foreach (CarMemory candidate in candidates)
            {
                if (ReferenceEquals(candidate, car) ||
                    candidate.ReadByte(ActiveBodyOffset) == 0 ||
                    candidate.ReadWord(CollisionCountOffset) != 0)
                {
                    continue;
                }

                FixedVector delta = FixedVectors.Subtract(
                    FixedVectors.Read(candidate, PositionOffset), FixedVectors.Read(car, PositionOffset));
                FixedVectors.Normalise(delta);
                int projection = FixedVectors.Dot(FixedVectors.Read(car, BasisRow2Offset), delta);

                if (projection <= MinimumProjection)
                {
                    continue;
                }

                int distance = FixedVectors.ChebyshevLikeDistance(
                    FixedVectors.Read(candidate, PositionOffset), FixedVectors.Read(car, PositionOffset));

                if (distance <= 0 || distance >= bestDistance || distance >= MaximumDistance)
                {
                    continue;
                }

                best = candidate;
                bestDistance = distance;
            }

            return best;
        }

        /// <summary>
        /// Exposes the source's distance measure so callers can assert candidate ordering. The
        /// routine returns the maximum absolute axis difference plus a quarter of the smaller
        /// absolute axis in source order; it does not take a square root.
        /// </summary>
        public static int Distance(CarMemory first, CarMemory second)
            => FixedVectors.ChebyshevLikeDistance(
                FixedVectors.Read(first, PositionOffset), FixedVectors.Read(second, PositionOffset));
    }
}