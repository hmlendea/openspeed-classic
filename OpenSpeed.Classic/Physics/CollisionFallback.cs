using System;

namespace OpenSpeed.Classic.Physics
{
    public static class CollisionFallback
    {
        private static readonly int[] angularScales = [0x3333, 0x5999];
        private static readonly int[] firstAxisScales = [0x10000, 0x3D999];
        private static readonly int[] thirdAxisScales = [0x10000, 0x1D999];
        private static readonly int[] velocityScales = [0x10000, 0x3B333];

        private static int AngularLimit => 0x26666;

        /// <summary>
        /// Applies sub_4540A0's fallback impulse, notification, and final angular clamp.
        /// Tick sign bits follow the executable's low-byte tests, not the older high-byte prose.
        /// </summary>
        public static void Apply(CarMemory car, int mode, int baseTick, Action<CarMemory> notifyImpact)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(notifyImpact);
            int index = 0;

            if (mode != 0 && (car[0x1F4] & 0x40) == 0)
            {
                index = 1;
            }

            int firstSign = 1;
            int thirdSign = 1;

            if ((baseTick & 4) != 0)
            {
                firstSign = -1;
            }

            if ((baseTick & 8) != 0)
            {
                thirdSign = -1;
            }

            ScaleAngular(car, X86Math.TruncatePowerOfTwo(angularScales[index], 8));
            AddAxis(car, 0xE8, 0x188, X86Math.TruncatePowerOfTwo(firstAxisScales[index] * firstSign, 8));
            AddAxis(car, 0xE8, 0x1A0, X86Math.TruncatePowerOfTwo(thirdAxisScales[index] * thirdSign, 8));
            AddVelocity(car, index);
            car.WriteWord(0x14C, unchecked((ushort)(car.ReadWord(0x14C) + 1)));
            notifyImpact(car);
            ClampAngular(car);
        }

        private static void ScaleAngular(CarMemory car, int scalar)
        {
            for (int component = 0; component < FixedMatrix.Dimension; component += 1)
            {
                int offset = 0xE8 + component * sizeof(int);
                car[offset] = X86Math.TruncatePowerOfTwo(unchecked(car[offset] * scalar), 8);
            }
        }

        private static void AddAxis(CarMemory car, int destination, int basisOffset, int scalar)
        {
            for (int component = 0; component < FixedMatrix.Dimension; component += 1)
            {
                int displacement = component * sizeof(int);
                int impulse = X86Math.TruncatePowerOfTwo(
                    unchecked(car[basisOffset + displacement] * scalar), 8);
                car[destination + displacement] = unchecked(car[destination + displacement] + impulse);
            }
        }

        private static void AddVelocity(CarMemory car, int index)
        {
            int difference = X86Math.Abs(unchecked(car[0xBC] - 0x2C0000));
            int adjustment = Math.Min(X86Math.TruncatePowerOfTwo(difference, 6), 0xE666);
            int scalar = X86Math.TruncatePowerOfTwo(unchecked(velocityScales[index] + adjustment), 8);
            AddAxis(car, 0xA8, 0x194, scalar);
        }

        private static void ClampAngular(CarMemory car)
        {
            for (int component = 0; component < FixedMatrix.Dimension; component += 1)
            {
                int offset = 0xE8 + component * sizeof(int);
                car[offset] = Math.Clamp(car[offset], -AngularLimit, AngularLimit);
            }
        }
    }
}