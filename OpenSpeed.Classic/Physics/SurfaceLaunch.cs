using System;

namespace OpenSpeed.Classic.Physics
{
    public static class SurfaceLaunch
    {
        private static int FlagsOffset => 0x1F4;

        private static int SurfaceOffset => 0x184;

        private static int SupportNormalOffset => 0x128;

        private static int CorrectionOffset => 0x15C;

        public static bool TryApply(CarMemory car, ReadOnlySpan<byte> launchFlags)
        {
            ArgumentNullException.ThrowIfNull(car);

            if ((car.ReadByte(FlagsOffset) & 0x10) != 0 ||
                launchFlags[car[SurfaceOffset]] == 0 ||
                car[SupportNormalOffset] >= 0xCCCC ||
                car[CorrectionOffset] > 0x7AE)
            {
                return false;
            }

            car[0xA8] = unchecked(car[0xA8] + car[0x124]);
            car[0xB0] = unchecked(car[0xB0] + car[0x12C]);
            car[0xAC] = unchecked(car[0xAC] - 0x14CCC);

            return true;
        }
    }
}