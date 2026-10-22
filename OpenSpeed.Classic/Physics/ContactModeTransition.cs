using System;

namespace OpenSpeed.Classic.Physics
{
    public static class ContactModeTransition
    {
        private static int MaximumActiveCars => 2;

        private static int DistanceThreshold => 0xF40000;

        private static int ContactModeOffset => 0x8C;

        private static int DistanceOffset => 0x88;

        /// <summary>
        /// Applies sub_462950's transition stores with explicit distance and native query dependencies.
        /// The distance callback owns any intermediate distance stores; resetContact represents sub_462E60.
        /// </summary>
        public static void Update(
            CarMemory car,
            int activeCarCount,
            Func<CarMemory, int> calculateDistance,
            Action<CarMemory> refreshRouteQuery,
            Action<CarMemory> refreshRouteBasis,
            Action<CarMemory> resetContact)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(calculateDistance);
            ArgumentNullException.ThrowIfNull(refreshRouteQuery);
            ArgumentNullException.ThrowIfNull(refreshRouteBasis);
            ArgumentNullException.ThrowIfNull(resetContact);

            if (activeCarCount > MaximumActiveCars)
            {
                car.WriteByte(ContactModeOffset, 0);
                car[DistanceOffset] = 0;

                return;
            }

            int distance = calculateDistance(car);
            car[DistanceOffset] = distance;

            if (distance > DistanceThreshold)
            {
                Enter(car);

                return;
            }

            if (car.ReadByte(ContactModeOffset) != 0)
            {
                Leave(car, refreshRouteQuery, refreshRouteBasis, resetContact);
            }

            car.WriteByte(ContactModeOffset, 0);
        }

        private static void Enter(CarMemory car)
        {
            if (car.ReadByte(ContactModeOffset) != 1)
            {
                car[0x180] = 1;
                car[0x184] = car[0x180];
            }

            car[0xE8] = 0;
            car[0xF0] = 0;
            car.WriteByte(ContactModeOffset, 1);
        }

        private static void Leave(
            CarMemory car,
            Action<CarMemory> refreshRouteQuery,
            Action<CarMemory> refreshRouteBasis,
            Action<CarMemory> resetContact)
        {
            car.WriteByte(ContactModeOffset, 0);
            refreshRouteQuery(car);
            refreshRouteBasis(car);
            resetContact(car);
            FixedVector normal = new();
            int height = ContactGeometry.CalculateContactHeight(car, normal);
            car[0x180] = 1;
            car[0x158] = 0;
            car[0x154] = height;
            car[0x184] = car[0x180];
            car[0x160] = 0;
            car[0xA0] = unchecked(car[0x154] + car[0x10C]);
            car[0x164] = 0;
            int correction = ContactGeometry.CalculateSupportCorrection(
                car, FixedVectors.Read(car, 0x124), FixedVectors.Read(car, 0x13C));
            car[0x15C] = correction;
            car[0x15C] = 0;
            car[0xA0] = unchecked(car[0xA0] - correction);
            car[0x17C] = 0;
            car[0x58C] = 0;
        }
    }
}