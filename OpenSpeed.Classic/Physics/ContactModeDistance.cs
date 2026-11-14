using System;

namespace OpenSpeed.Classic.Physics
{
    public static class ContactModeDistance
    {
        private static int RouteIndexOffset => 0x14;

        private static int PositionFirstOffset => 0x9C;

        private static int PositionThirdOffset => 0xA4;

        private static int RouteDistanceShift => 18;

        /// <summary>
        /// Calculates the single-active-car distance from sub_462950 without changing either body.
        /// </summary>
        public static int Calculate(CarMemory car, CarMemory activeCar, int recordCount)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(activeCar);
            int planarDistance = CalculatePlanarDistance(car, activeCar);
            int routeDistance = CalculateRouteDistance(car, activeCar, recordCount);

            return Math.Max(planarDistance, unchecked(routeDistance << RouteDistanceShift));
        }

        /// <summary>
        /// Calculates the two-active-car distance, minimising planar and route distances independently.
        /// </summary>
        public static int Calculate(
            CarMemory car,
            CarMemory activeCar,
            CarMemory otherActiveCar,
            int recordCount)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(activeCar);
            ArgumentNullException.ThrowIfNull(otherActiveCar);
            int planarDistance = Math.Min(
                CalculatePlanarDistance(car, activeCar),
                CalculatePlanarDistance(car, otherActiveCar));
            int routeDistance = Math.Min(
                CalculateRouteDistance(car, activeCar, recordCount),
                CalculateRouteDistance(car, otherActiveCar, recordCount));

            return Math.Max(planarDistance, unchecked(routeDistance << RouteDistanceShift));
        }

        private static int CalculatePlanarDistance(CarMemory car, CarMemory activeCar)
        {
            int first = X86Math.Abs(unchecked(car[PositionFirstOffset] - activeCar[PositionFirstOffset]));
            int third = X86Math.Abs(unchecked(car[PositionThirdOffset] - activeCar[PositionThirdOffset]));

            return unchecked(Math.Max(first, third) + (Math.Min(first, third) >> 2));
        }

        private static int CalculateRouteDistance(CarMemory car, CarMemory activeCar, int recordCount)
            => RouteIndexSearch.CalculateCircularDistance(
                car[RouteIndexOffset], activeCar[RouteIndexOffset], recordCount);
    }
}