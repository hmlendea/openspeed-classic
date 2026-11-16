using System;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public sealed class FixedPointCarSolver
    {
        private const int AirborneHeight = 0x199A;
        private const int ContactDecay = 0x1F4;
        private const int HandbrakeThreshold = 0x8000;
        private const int LowHeightAccelerationDamping = unchecked((int)0xFAE1);
        private const int VelocityDamping = unchecked((int)0xF0A3);

        public void Update(FixedPointCarState car)
        {
            ArgumentNullException.ThrowIfNull(car);

            if (car.Height < AirborneHeight)
            {
                car[0x310] = 0;
                car[0x324] = 0;
                car[0x328] = 0;
                car[0x2F4] = 0;
                car[0x2F0] = car[0x2F0] > ContactDecay
                    ? unchecked(car[0x2F0] - ContactDecay)
                    : 0;

                if (car.HandbrakeGate < HandbrakeThreshold)
                {
                    ApplyLowHeightDamping(car);
                }

                return;
            }

            MainSolver(car);
        }

        public static void ProjectBasis(
            FixedPointCarState car,
            int inputOffset,
            int outputOffset)
        {
            ArgumentNullException.ThrowIfNull(car);

            for (int row = 0; row < 3; row++)
            {
                int result = 0;
                for (int column = 0; column < 3; column++)
                {
                    result = FixedPointArithmetic.Add(
                        result,
                        FixedPointArithmetic.MultiplyQ16(
                            car[inputOffset + column * 4],
                            car[0x188 + (row * 3 + column) * 4]));
                }

                car[outputOffset + row * 4] = result;
            }
        }

        private static void ApplyLowHeightDamping(FixedPointCarState car)
        {
            car[0xA8] = FixedPointArithmetic.MultiplyQ16(car[0xA8], VelocityDamping);
            car[0xAC] = FixedPointArithmetic.MultiplyQ16(car[0xAC], VelocityDamping);
            car[0xB0] = FixedPointArithmetic.MultiplyQ16(car[0xB0], VelocityDamping);

            if (car.Height < 0x3333)
            {
                car[0xEC] = FixedPointArithmetic.MultiplyQ16(
                    car[0xEC],
                    LowHeightAccelerationDamping);
            }
        }

        private static void MainSolver(FixedPointCarState car)
        {
            car[0x2A4] = 0;
            car[0x2A8] = 0;
            car[0x2AC] = 0;
            ProjectBasis(car, 0xA8, 0x2B0);
            ProjectBasis(car, 0xE8, 0x2BC);
            car[0x310] = car[0x2B8] > 0x50000
                ? FixedPointArithmetic.Divide(car[0x2B0], car[0x2B8])
                : 0;
        }
    }
}