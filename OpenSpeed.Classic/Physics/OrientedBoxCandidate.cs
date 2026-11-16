using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates the four oriented-box candidate helpers used by <c>sub_455100</c>:
    /// <c>sub_457CD0</c>, <c>sub_4577D0</c>, <c>sub_457A10</c>, and <c>sub_457590</c>.
    /// The first probe is expressed in the first box's frame and tested against the first
    /// box's extents, emitting the second box's corner; the second probe mirrors that with
    /// a negated centre delta, the second box's extents, and the first box's corner. The
    /// <c>sub_4577D0</c> and <c>sub_457590</c> variants additionally emit a world normal.
    /// </summary>
    public static class OrientedBoxCandidate
    {
        private static int PositionOffset => 0x9C;

        private static int ExtentOffset => 0x108;

        /// <summary>
        /// Runs <c>sub_457CD0</c>: first-box probe, second-box corner, no normal.
        /// </summary>
        public static bool TryApplyFirstProbe(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            ReadOnlySpan<int> coefficients,
            out FixedVector? point)
            => TryApplyFirstProbeCore(firstCar, secondCar, scratch, coefficients, out point, out _);

        /// <summary>
        /// Runs <c>sub_4577D0</c>: <c>sub_457CD0</c> plus the second box's world normal.
        /// </summary>
        public static bool TryApplyFirstProbeWithNormal(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            ReadOnlySpan<int> coefficients,
            out FixedVector? point,
            out FixedVector? normal)
            => TryApplyFirstProbeCore(firstCar, secondCar, scratch, coefficients, out point, out normal);

        /// <summary>
        /// Runs <c>sub_457A10</c>: second-box probe, first-box corner, no normal.
        /// </summary>
        public static bool TryApplySecondProbe(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            ReadOnlySpan<int> coefficients,
            out FixedVector? point)
            => TryApplySecondProbeCore(firstCar, secondCar, scratch, coefficients, out point, out _);

        /// <summary>
        /// Runs <c>sub_457590</c>: <c>sub_457A10</c> plus the first box's negated world normal.
        /// </summary>
        public static bool TryApplySecondProbeWithNormal(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            ReadOnlySpan<int> coefficients,
            out FixedVector? point,
            out FixedVector? normal)
            => TryApplySecondProbeCore(firstCar, secondCar, scratch, coefficients, out point, out normal);

        private static bool TryApplyFirstProbeCore(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            ReadOnlySpan<int> coefficients,
            out FixedVector? point,
            out FixedVector? normal)
        {
            ArgumentNullException.ThrowIfNull(firstCar);
            ArgumentNullException.ThrowIfNull(secondCar);
            ArgumentNullException.ThrowIfNull(scratch);
            OrientedBoxCandidateArithmetic.ValidateCoefficients(coefficients);
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

        private static bool TryApplySecondProbeCore(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            ReadOnlySpan<int> coefficients,
            out FixedVector? point,
            out FixedVector? normal)
        {
            ArgumentNullException.ThrowIfNull(firstCar);
            ArgumentNullException.ThrowIfNull(secondCar);
            ArgumentNullException.ThrowIfNull(scratch);
            OrientedBoxCandidateArithmetic.ValidateCoefficients(coefficients);
            point = null;
            normal = null;
            int[] negatedDelta = OrientedBoxCandidateArithmetic.Negate(scratch.DeltaInSecond);
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
    }
}