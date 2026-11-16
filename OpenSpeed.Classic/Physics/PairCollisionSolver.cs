using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Composes the recovered car-pair collision chain: <c>sub_4543F0</c> contact generation
    /// through <see cref="OrientedBoxContactResolver"/>, <c>sub_455B60</c> impulses through
    /// <see cref="CollisionImpulse"/>, the <c>sub_46A130</c> motion refresh, the
    /// <c>sub_4540A0</c> fallback, and the <c>sub_453F30</c> pair-completion race event.
    /// </summary>
    public static class PairCollisionSolver
    {
        private const int AdditionalImpulseLimit = 8;

        private const int EventMagnitudeOffset = 0x160;

        private static int FirstAddress => 0x1000;

        private static int SecondAddress => 0x2000;

        /// <summary>
        /// Executes <c>sub_453F30</c>. Returns true when the first contact was generated and
        /// its impulse accepted, matching the routine's <c>eax</c>.
        /// </summary>
        public static bool Resolve(
            CarMemory first,
            CarMemory second,
            OrientedBoxScratch scratch,
            Action<CarMemory> refreshMotion,
            Action<CarMemory> applyFallback,
            int raceType)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(scratch);
            ArgumentNullException.ThrowIfNull(refreshMotion);
            ArgumentNullException.ThrowIfNull(applyFallback);
            first[EventMagnitudeOffset] = 0;
            second[EventMagnitudeOffset] = 0;
            FixedVector normal = default;
            OrientedBoxContactResolver.Result contact = OrientedBoxContactResolver.Resolve(
                first, second, scratch, default, normal);

            if (contact.Iterations == 0)
            {
                return false;
            }

            if (!CollisionImpulse.ApplyPair(first, second, contact.Point, contact.Normal, SecondAddress, FirstAddress))
            {
                return false;
            }

            refreshMotion(first);
            refreshMotion(second);
            int remaining = AdditionalImpulseLimit;

            while (true)
            {
                contact = OrientedBoxContactResolver.Resolve(first, second, scratch, default, contact.Normal);

                if (contact.Iterations == 0 || remaining <= 0)
                {
                    break;
                }

                bool wasApplied = CollisionImpulse.ApplyPair(
                    first, second, contact.Point, contact.Normal, SecondAddress, FirstAddress);
                remaining -= 1;

                if (!wasApplied)
                {
                    return true;
                }

                refreshMotion(first);
                refreshMotion(second);

                if (remaining == 0)
                {
                    applyFallback(first);
                    applyFallback(second);
                }
            }

            PairCollisionRaceEvent.ShouldLatch(first, second, raceType);

            return true;
        }
    }
}