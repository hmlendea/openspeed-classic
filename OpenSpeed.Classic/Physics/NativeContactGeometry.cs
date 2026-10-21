using System;

namespace OpenSpeed.Classic.Physics
{
    public static class NativeContactGeometry
    {
        private static int ActiveBodyOffset => 0x8D;

        private static int ContactModeOffset => 0x8C;

        private static int FlagsOffset => 0x1F4;

        private static int ContactYOffset => 0x140;

        private static int WorldYOffset => 0xA0;

        private static int Word14COffset => 0x14C;

        private static int Byte8EOffset => 0x8E;

        private static int VerticalVelocityOffset => 0xAC;

        public static bool TryResolveContactMode(CarMemory car)
        {
            ArgumentNullException.ThrowIfNull(car);

            if (car.ReadByte(ActiveBodyOffset) == 0 ||
                car.ReadByte(ContactModeOffset) == 0)
            {
                return false;
            }

            int contactHeight = car[ContactYOffset];

            if ((car[FlagsOffset] & 0x40) == 0)
            {
                FixedVector normal = new();
                contactHeight = unchecked(
                    ContactGeometry.CalculateContactHeight(car, normal) + car[0x10C]);
            }

            car[WorldYOffset] = contactHeight;
            car.WriteWord(Word14COffset, 0);
            car.WriteByte(Byte8EOffset, 0);
            car[VerticalVelocityOffset] = 0;

            return true;
        }
    }
}