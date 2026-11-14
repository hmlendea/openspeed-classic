using System;

namespace OpenSpeed.Classic.Physics
{
    public static class OrientedBoxScratchCalculator
    {
        private static int PositionOffset => 0x9C;

        private static int PrimaryBasisOffset => 0xC4;

        private static int ExtentOffset => 0x108;

        /// <summary>
        /// Precomputes sub_4543F0's rotation, extent projections, and centre deltas.
        /// Rotation cells are row-to-row dot products. For cell [i,j], the first
        /// extent group multiplies B.extent[j] and the second multiplies A.extent[i].
        /// </summary>
        public static void Calculate(CarMemory first, CarMemory second, OrientedBoxScratch scratch)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(scratch);
            FixedVector delta = FixedVectors.Subtract(
                FixedVectors.Read(second, PositionOffset), FixedVectors.Read(first, PositionOffset));

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                FixedVector firstRow = ReadRow(first, row);
                FixedVector secondRow = ReadRow(second, row);
                scratch.DeltaInFirst[row] = FixedVectors.Dot(firstRow, delta);
                scratch.DeltaInSecond[row] = FixedVectors.Dot(secondRow, delta);

                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    scratch.Rotation[row * FixedMatrix.Dimension + column] =
                        FixedVectors.Dot(firstRow, ReadRow(second, column));
                }
            }

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                int firstExtent = first[ExtentOffset + row * sizeof(int)];

                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    int cell = row * FixedMatrix.Dimension + column;
                    int rotation = scratch.Rotation[cell];
                    int secondExtent = second[ExtentOffset + column * sizeof(int)];
                    scratch.SecondExtentInFirst[cell] = X86Math.MultiplyQ16(rotation, secondExtent);
                    scratch.FirstExtentInSecond[cell] = X86Math.MultiplyQ16(rotation, firstExtent);                }
            }
        }

        /// <summary>
        /// Recomputes only the centre deltas, which is what <c>sub_4543F0</c> repeats on
        /// every iteration of its correction loop. Rotation cells and extent projections
        /// stay at their single pre-loop values with the original routine.
        /// </summary>
        public static void UpdateDeltas(CarMemory first, CarMemory second, OrientedBoxScratch scratch)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            ArgumentNullException.ThrowIfNull(scratch);
            FixedVector delta = FixedVectors.Subtract(
                FixedVectors.Read(second, PositionOffset), FixedVectors.Read(first, PositionOffset));

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                scratch.DeltaInFirst[row] = FixedVectors.Dot(ReadRow(first, row), delta);
                scratch.DeltaInSecond[row] = FixedVectors.Dot(ReadRow(second, row), delta);
            }
        }

        private static FixedVector ReadRow(CarMemory car, int row)
            => FixedVectors.Read(car, PrimaryBasisOffset + row * FixedMatrix.Dimension * sizeof(int));
    }
}