using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// The two 12-dword contact-state tables that the alternate contact routines load onto their
    /// stack before selecting an entry by the signed response state <c>+0x2F4</c>. They are
    /// executable literals, not asset data.
    /// </summary>
    public static class TyreResponseTables
    {
        private static readonly int[] responseStates =
        [
            X86Math.One, X86Math.One, 0x9999, 0xE666, 0x18000, X86Math.One,
            0x1999, 0x1999, 0x51E, 0xCCC, 0x1999, 0
        ];

        private static readonly int[] capacityStates =
        [
            0x1999, X86Math.One, 0x4CCC, 0x4CCC, 0x8000, 0,
            0, X86Math.One, 0x28000, 0x30000, 0x40000, X86Math.One
        ];

        /// <summary>
        /// The table at <c>dword_4646A0</c>, indexed by the absolute response state.
        /// </summary>
        public static ReadOnlySpan<int> ResponseStates => responseStates;

        /// <summary>
        /// The table at <c>dword_4646D0</c>, indexed by the absolute response state.
        /// </summary>
        public static ReadOnlySpan<int> CapacityStates => capacityStates;

        /// <summary>
        /// Reads <see cref="ResponseStates"/> with the source's caller convention: the caller
        /// indexes with the absolute response state and falls back to entry zero when the state
        /// is zero.
        /// </summary>
        public static int ResponseState(int state)
        {
            int index = X86Math.Abs(state);

            if (index == 0)
            {
                return responseStates[0];
            }

            return index < responseStates.Length ? responseStates[index] : 0;
        }

        /// <summary>
        /// Reads <see cref="CapacityStates"/> with the same convention as
        /// <see cref="ResponseState"/>.
        /// </summary>
        public static int CapacityState(int state)
        {
            int index = X86Math.Abs(state);

            if (index == 0)
            {
                return capacityStates[0];
            }

            return index < capacityStates.Length ? capacityStates[index] : 0;
        }
    }
}