    using System;

namespace OpenSpeed.Classic.Physics
{
    public static class PairCollisionResolver
    {
        private static int AdditionalImpulseLimit => 8;

        private static int EventMagnitudeOffset => 0x160;

        /// <summary>
        /// Executes sub_453F30's iterative contact/impulse order with explicit native dependencies.
        /// Contact generation owns the contact point and normal shared with the impulse callback.
        /// </summary>
        public static bool Resolve(
            CarMemory first,
            CarMemory second,
            Func<CarMemory, CarMemory, bool> generateContact,
            Func<CarMemory, CarMemory, bool> applyImpulse,
            Action<CarMemory> refreshMotion,
            Action<CarMemory> applyFallback,
            Action<CarMemory, CarMemory> completePair)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(generateContact);
            ArgumentNullException.ThrowIfNull(applyImpulse);
            ArgumentNullException.ThrowIfNull(refreshMotion);
            ArgumentNullException.ThrowIfNull(applyFallback);
            ArgumentNullException.ThrowIfNull(completePair);
            first[EventMagnitudeOffset] = 0;
            second[EventMagnitudeOffset] = 0;

            if (!generateContact(first, second) || !applyImpulse(first, second))
            {
                return false;
            }

            refreshMotion(first);
            refreshMotion(second);

            return ResolveRemaining(
                first, second, generateContact, applyImpulse, refreshMotion, applyFallback, completePair);
        }

        private static bool ResolveRemaining(
            CarMemory first,
            CarMemory second,
            Func<CarMemory, CarMemory, bool> generateContact,
            Func<CarMemory, CarMemory, bool> applyImpulse,
            Action<CarMemory> refreshMotion,
            Action<CarMemory> applyFallback,
            Action<CarMemory, CarMemory> completePair)
        {
            int remaining = AdditionalImpulseLimit;

            while (generateContact(first, second) && remaining > 0)
            {
                bool wasApplied = applyImpulse(first, second);
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

            completePair(first, second);

            return true;
        }
    }
}