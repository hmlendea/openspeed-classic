using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates the <c>sub_457CD0</c> and <c>sub_457A10</c> candidate helpers used by
    /// <c>sub_455100</c>. Both build a probe from the centre delta plus the other box's
    /// extents projected through the rotation, test it inclusively against one box's
    /// extents, and, when accepted, emit that same box's extreme corner and the signed
    /// world normal.
    /// </summary>
    public static class OrientedBoxFaceCandidate
    {
        private static int PositionOffset => 0x9C;

        private static int ExtentOffset => 0x108;

        /// <summary>
        /// Runs <c>sub_457CD0</c>: probe in the first box's frame tested against the first
        /// box's extents, emitting the second box's corner and the positive normal.
        /// </summary>
        public static bool TryApplyFirstProbe(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            ReadOnlySpan<int> coefficients,
            out FixedVector? point,
            out FixedVector? normal)
        {
            ArgumentNullException.ThrowIfNull(firstCar);
            ArgumentNullException.ThrowIfNull(secondCar);
            ValidateArguments(scratch, coefficients);
            point = null;
            normal = null;
            int[] probe = OrientedBoxCandidateArithmetic.CalculateProbe(
                scratch.DeltaInFirst, scratch.SecondExtentInFirst, coefficients);

            if (!OrientedBoxCandidateArithmetic.IsInsideExtents(firstCar, probe, ExtentOffset))
            {
                return false;
            }

            point = OrientedBoxCandidateArithmetic.CalculateCorner(
                secondCar, coefficients, PositionOffset, ExtentOffset);
            normal = OrientedBoxCandidateArithmetic.CalculateNormal(secondCar, coefficients, 1);

            return true;
        }

        /// <summary>
        /// Runs <c>sub_457A10</c>: probe in the second box's frame tested against the second
        /// box's extents, emitting the first box's corner and the negated normal.
        /// </summary>
        public static bool TryApplySecondProbe(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            ReadOnlySpan<int> coefficients,
            out FixedVector? point,
            out FixedVector? normal)
        {
            ArgumentNullException.ThrowIfNull(firstCar);
            ArgumentNullException.ThrowIfNull(secondCar);
            ValidateArguments(scratch, coefficients);
            point = null;
            normal = null;
            int[] negatedDelta = new int[FixedMatrix.Dimension];

            for (int axis = 0; axis < FixedMatrix.Dimension; axis += 1)
            {
                negatedDelta[axis] = unchecked(-scratch.DeltaInSecond[axis]);
            }

            int[] probe = OrientedBoxCandidateArithmetic.CalculateProbe(
                negatedDelta, scratch.FirstExtentInSecond, coefficients);

            if (!OrientedBoxCandidateArithmetic.IsInsideExtents(secondCar, probe, ExtentOffset))
            {
                return false;
            }

            point = OrientedBoxCandidateArithmetic.CalculateCorner(
                firstCar, coefficients, PositionOffset, ExtentOffset);
            normal = OrientedBoxCandidateArithmetic.CalculateNormal(firstCar, coefficients, -1);

            return true;
        }

        private static void ValidateArguments(OrientedBoxScratch scratch, ReadOnlySpan<int> coefficients)
        {
            ArgumentNullException.ThrowIfNull(scratch);

            if (coefficients.Length != FixedMatrix.Dimension)
            {
                throw new ArgumentException(
                    $"The candidate requires {FixedMatrix.Dimension} coefficients, but {coefficients.Length} were supplied.",
                    nameof(coefficients));
            }
        }
    }
}