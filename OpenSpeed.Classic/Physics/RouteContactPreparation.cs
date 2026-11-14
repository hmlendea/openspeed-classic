using System;

namespace OpenSpeed.Classic.Physics
{
    public static class RouteContactPreparation
    {
        private static int ActiveBodyOffset => 0x8D;

        private static int ContactTimerOffset => 0x17C;

        private static int ExtensionOffset => 0x18;

        private static int RouteRefreshOffset => 0x76;

        private static int ContactModeOffset => 0x8C;

        private static int QueryResultOffset => 0x180;

        private static int SurfaceOffset => 0x184;

        /// <summary>
        /// Dispatches the current position and native query mode to the supplied route query.
        /// The caller owns the query context corresponding to the car's byte offset 0x08.
        /// </summary>
        public static void DispatchRouteQuery(CarMemory car, Action<FixedVector, int> queryRoute)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(queryRoute);
            int queryMode = 1 - car.ReadByte(ContactModeOffset);
            queryRoute(FixedVectors.Read(car, 0x9C), queryMode);
        }

        public static void Update(
            CarMemory car,
            Action<CarMemory> transitionContactMode,
            Action<CarMemory> refreshRouteQuery,
            Action<CarMemory> refreshRouteBasis,
            Func<CarMemory, int> querySurface)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(transitionContactMode);
            ArgumentNullException.ThrowIfNull(refreshRouteQuery);
            ArgumentNullException.ThrowIfNull(refreshRouteBasis);
            ArgumentNullException.ThrowIfNull(querySurface);

            if (car.ReadByte(ActiveBodyOffset) == 0)
            {
                return;
            }

            transitionContactMode(car);

            if (car[ContactTimerOffset] != 0 && car[ExtensionOffset] == 0)
            {
                return;
            }

            refreshRouteQuery(car);

            if (car.ReadWord(RouteRefreshOffset) != 0)
            {
                refreshRouteBasis(car);
            }

            if (car.ReadByte(ContactModeOffset) != 0)
            {
                return;
            }

            int queryResult = querySurface(car);
            car[QueryResultOffset] = queryResult;
            car[SurfaceOffset] = queryResult & 0x3F;
        }
    }
}