using System;
using System.Collections.Generic;

namespace OpenSpeed.Classic.Physics
{
    public static class PairCollisionBroadPhase
    {
        private static int AngularPreScale => 0x6487E;

        private static int AngularPostScale => 0x28BE;

        private static int AngularLimit => 0x1CCCC;

        private static int ContactModeOffset => 0x8C;

        private static int ActiveBodyOffset => 0x8D;

        private static int CoolDownOffset => 0x16C;

        private static int DistanceMetricOffset => 0x88;

        private static int MaximumDistanceMetric => 0x640000;

        private static int RadiusOffset => 0x114;

        private static int ThirdOffset => 0xA4;

        private static int FirstOffset => 0x9C;

        private static int SecondOffset => 0xA0;

        /// <summary>
        /// Executes sub_453860's broad phase against earlier cars in the supplied order.
        /// The resolver callback owns contact generation, impulses, and completion semantics;
        /// refreshMotion corresponds to the final sub_46A130 call on the current car.
        /// </summary>
        public static int Update(
            CarMemory car,
            IReadOnlyList<CarMemory> earlierCars,
            Func<CarMemory, CarMemory, bool> resolvePair,
            Action<CarMemory> refreshMotion)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(earlierCars);
            ArgumentNullException.ThrowIfNull(resolvePair);
            ArgumentNullException.ThrowIfNull(refreshMotion);
            int coolDown = car[CoolDownOffset];

            if (coolDown > 0)
            {
                car[CoolDownOffset] = unchecked(coolDown - 1);
            }

            if (car.ReadByte(ContactModeOffset) != 0 || car.ReadByte(ActiveBodyOffset) == 0)
            {
                return 0;
            }

            bool isEnabled = true;

            if (car[CoolDownOffset] != 0 || car[DistanceMetricOffset] > MaximumDistanceMetric)
            {
                isEnabled = false;
            }

            int resolvedPairs = 0;

            if (isEnabled)
            {
                foreach (CarMemory earlierCar in earlierCars)
                {
                    if (!Intersects(car, earlierCar))
                    {
                        continue;
                    }

                    ScaleAngular(car, AngularPreScale);
                    ScaleAngular(earlierCar, AngularPreScale);
                    resolvePair(car, earlierCar);
                    ScaleAngular(car, AngularPostScale);
                    ScaleAngular(earlierCar, AngularPostScale);
                    ClampAngular(car);
                    ClampAngular(earlierCar);
                    resolvedPairs += 1;
                }
            }

            refreshMotion(car);

            return resolvedPairs;
        }

        private static bool Intersects(CarMemory car, CarMemory earlierCar)
        {
            int radius = unchecked(car[RadiusOffset] + earlierCar[RadiusOffset]);

            if (X86Math.Abs(unchecked(car[ThirdOffset] - earlierCar[ThirdOffset])) >= radius)
            {
                return false;
            }

            if (X86Math.Abs(unchecked(car[FirstOffset] - earlierCar[FirstOffset])) >= radius)
            {
                return false;
            }

            return X86Math.Abs(unchecked(car[SecondOffset] - earlierCar[SecondOffset])) < radius;
        }

        private static void ScaleAngular(CarMemory car, int scale)
        {
            FixedVectors.Write(car, 0xE8, FixedVectors.Scale(FixedVectors.Read(car, 0xE8), scale));
        }

        private static void ClampAngular(CarMemory car)
        {
            FixedVector angular = FixedVectors.Read(car, 0xE8);
            angular.First = Math.Clamp(angular.First, -AngularLimit, AngularLimit);
            angular.Second = Math.Clamp(angular.Second, -AngularLimit, AngularLimit);
            angular.Third = Math.Clamp(angular.Third, -AngularLimit, AngularLimit);
            FixedVectors.Write(car, 0xE8, angular);
        }
    }
}