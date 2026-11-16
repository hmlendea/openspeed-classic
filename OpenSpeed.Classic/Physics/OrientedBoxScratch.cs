using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Holds the process-wide scratch matrices sub_4543F0 precomputes before its
    /// iterative candidate loop. Values are index [row * Dimension + column].
    /// </summary>
    public sealed class OrientedBoxScratch
    {
        public int[] Rotation { get; } = new int[FixedMatrix.CellCount];

        public int[] SecondExtentInFirst { get; } = new int[FixedMatrix.CellCount];

        public int[] FirstExtentInSecond { get; } = new int[FixedMatrix.CellCount];

        public int[] DeltaInFirst { get; } = new int[FixedMatrix.Dimension];

        public int[] DeltaInSecond { get; } = new int[FixedMatrix.Dimension];

        public void Clear()
        {
            Array.Clear(Rotation);
            Array.Clear(FirstExtentInSecond);
            Array.Clear(SecondExtentInFirst);
            Array.Clear(DeltaInFirst);
            Array.Clear(DeltaInSecond);
        }
    }
}
