using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates <c>sub_450890</c>: the surface-launch impulse applied from the scheduled
    /// surface/recovery callback. The launch table is a dword per surface index; the gate treats
    /// a zero entry as "no launch for this surface". The impulse adds the contact-basis lateral
    /// components to the horizontal velocity and subtracts the fixed vertical constant.
    /// </summary>
    public static class SurfaceLaunch
    {
        private static int FlagsOffset => 0x1F4;

        private static int SurfaceOffset => 0x184;

        private static int SupportNormalOffset => 0x128;

        private static int CorrectionOffset => 0x15C;

        private static int VerticalLaunch => 0x14CCC;

        /// <summary>
        /// Applies the launch impulse using the recovered <see cref="PhysicsTables.SurfaceLaunchFlags"/>
        /// table. Returns false without writes when any gate fails or the surface index is outside
        /// the recovered table.
        /// </summary>
        public static bool TryApply(CarMemory car) => TryApply(car, PhysicsTables.SurfaceLaunchFlags);

        /// <summary>
        /// Applies the launch impulse with an explicit flag table. Each entry is nonzero for a
        /// launching surface, matching the source's <c>dword_4D6070</c> comparison.
        /// </summary>
        public static bool TryApply(CarMemory car, ReadOnlySpan<int> launchFlags)
        {
            ArgumentNullException.ThrowIfNull(car);

            int surface = car[SurfaceOffset];

            if ((car.ReadByte(FlagsOffset) & 0x10) != 0 ||
                surface < 0 ||
                surface >= launchFlags.Length ||
                launchFlags[surface] == 0 ||
                car[SupportNormalOffset] >= 0xCCCC ||
                car[CorrectionOffset] > 0x7AE)
            {
                return false;
            }

            car[0xA8] = unchecked(car[0xA8] + car[0x124]);
            car[0xB0] = unchecked(car[0xB0] + car[0x12C]);
            car[0xAC] = unchecked(car[0xAC] - VerticalLaunch);

            return true;
        }
    }
}