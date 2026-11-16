using System;

namespace OpenSpeed.Classic.Physics
{
    public static class RouteContactReset
    {
        private static int EventMagnitudeOffset => 0x160;

        private static int EventPartnerOffset => 0x164;

        /// <summary>
        /// Applies sub_462E60's conditional reset with its native correction predicate supplied explicitly.
        /// A successful correction clears magnitude and partner before refreshing the route query and basis.
        /// </summary>
        public static void Update(
            CarMemory car,
            Func<CarMemory, bool> correctRouteContact,
            Action<CarMemory> refreshRouteQuery,
            Action<CarMemory> refreshRouteBasis)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(correctRouteContact);
            ArgumentNullException.ThrowIfNull(refreshRouteQuery);
            ArgumentNullException.ThrowIfNull(refreshRouteBasis);

            if (!correctRouteContact(car))
            {
                return;
            }

            car[EventMagnitudeOffset] = 0;
            car[EventPartnerOffset] = 0;
            refreshRouteQuery(car);
            refreshRouteBasis(car);
        }
    }
}