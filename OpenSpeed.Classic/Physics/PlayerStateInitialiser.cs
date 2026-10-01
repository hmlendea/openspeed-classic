using System;

namespace OpenSpeed.Classic.Physics
{
    public static class PlayerStateInitialiser
    {
        private static int LinearCoefficient => 0xA0000;

        private static int AngularCoefficient => 0xF0000;

        private static byte NeutralGear => 1;

        private static readonly int[] ClearedDwords =
        [
            0x18, 0x24, 0xFC, 0x104, 0x158, 0x15C, 0xC0, 0x98,
            0x200, 0x204, 0x2E0, 0x2E4, 0x2E8, 0x314, 0x31C,
            0x320, 0x324, 0x328, 0x32C, 0x360, 0x364, 0x368, 0x17C
        ];

        public static void Initialise(CarMemory car)
            => Initialise(car, null, null);

        public static void Initialise(CarMemory car, FixedVector? extents, int? radius)
        {
            ArgumentNullException.ThrowIfNull(car);

            if (car[0xB4] == 0)
            {
                car[0xB4] = LinearCoefficient;
                car[0xB8] = X86Math.DivideQ16(X86Math.One, LinearCoefficient);
            }

            if (car[0xF4] == 0)
            {
                car[0xF4] = AngularCoefficient;
                car[0xF8] = X86Math.DivideQ16(X86Math.One, AngularCoefficient);
            }

            foreach (int offset in ClearedDwords)
            {
                car[offset] = 0;
            }

            ClearRange(car, 0xA8, 0xB0);
            ClearRange(car, 0xE8, 0xF0);
            ClearRange(car, 0x160, 0x16C);
            ClearRange(car, 0x28C, 0x2D0);
            ClearRange(car, 0x2F0, 0x310);
            ClearRange(car, 0x578, 0x590);

            for (int offset = 0x2D4; offset <= 0x2DE; offset += 1)
            {
                car.WriteByte(offset, 0);
            }

            car.WriteByte(0x2D6, NeutralGear);
            car.WriteByte(0x2D9, NeutralGear);
            car.WriteByte(0x2DA, NeutralGear);
            car.WriteByte(0x8C, 0);
            car.WriteByte(0x8D, 1);
            car.WriteByte(0x8E, 0);
            car.WriteByte(0x8F, 0);
            car.WriteByte(0x90, 0);
            car.WriteWord(0x14C, 0);
            car.WriteWord(0x14E, 0);
            car[0x94] = car[0x100] = car[0x218] = X86Math.One;
            car[0x180] = car[0x184] = 1;
            car[0x150] = 0x640000;

            if (extents is not null)
            {
                FixedVectors.Write(car, 0x108, extents);
            }

            if (radius.HasValue)
            {
                car[0x114] = radius.Value;
            }
        }

        private static void ClearRange(CarMemory car, int first, int last)
        {
            for (int offset = first; offset <= last; offset += sizeof(int))
            {
                car[offset] = 0;
            }
        }
    }
}