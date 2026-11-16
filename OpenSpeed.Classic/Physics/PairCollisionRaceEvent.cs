using System;

namespace OpenSpeed.Classic.Physics
{
    public static class PairCollisionRaceEvent
    {
        private static int ActionOffset => 0x2DD;

        private static int RoleOffset => 0x1F4;

        private static int PlayerRoleMask => 4;

        private static int SpecialRoleMask => 0x40;

        private static int LongitudinalOffset => 0x2B8;

        private static int ReverseThreshold => -0xA0000;

        /// <summary>
        /// Returns whether sub_453F30's completion branch sets dword_4D8514.
        /// The caller must latch a true result without clearing an already-set race event.
        /// </summary>
        public static bool ShouldLatch(CarMemory first, CarMemory second, int raceType)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);

            if (first.ReadByte(ActionOffset) == 0 && second.ReadByte(ActionOffset) == 0)
            {
                return false;
            }

            if (raceType != 6 && raceType != 7)
            {
                return false;
            }

            return Qualifies(first, second) || Qualifies(second, first);
        }

        private static bool Qualifies(CarMemory player, CarMemory specialCar)
            => (player.ReadByte(RoleOffset) & PlayerRoleMask) != 0 &&
                (specialCar.ReadByte(RoleOffset) & SpecialRoleMask) != 0 &&
                player[LongitudinalOffset] < ReverseThreshold;
    }
}