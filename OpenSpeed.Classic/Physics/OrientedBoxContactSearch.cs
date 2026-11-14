using System;
using System.Collections.Generic;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates the candidate search performed by <c>sub_455100</c>: six axis tuples for
    /// each corner-only probe, then eight sign triples for each corner-plus-normal probe,
    /// in source order. The first accepted candidate terminates the search.
    /// </summary>
    public static class OrientedBoxContactSearch
    {
        private static readonly int[][] axisTuples =
        [
            [1, 0, 0],
            [1, 1, 0],
            [1, 0, 1],
            [-1, 0, 0],
            [-1, -1, 0],
            [-1, 0, -1]
        ];

        private static readonly int[][] signTriples =
        [
            [1, 1, 1],
            [1, 1, -1],
            [1, -1, 1],
            [1, -1, -1],
            [-1, 1, 1],
            [-1, 1, -1],
            [-1, -1, 1],
            [-1, -1, -1]
        ];

        /// <summary>
        /// Searches for the first accepted contact candidate and reports which source
        /// routine produced it.
        /// </summary>
        public static bool TrySearch(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            out OrientedBoxContactCandidate result)
        {
            ArgumentNullException.ThrowIfNull(firstCar);
            ArgumentNullException.ThrowIfNull(secondCar);
            ArgumentNullException.ThrowIfNull(scratch);
            result = default;

            if (TryAcceptCornerOnly(firstCar, secondCar, scratch, out result))
            {
                return true;
            }

            return TryAcceptWithNormal(firstCar, secondCar, scratch, out result);
        }

        private static bool TryAcceptCornerOnly(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            out OrientedBoxContactCandidate result)
        {
            foreach (int[] coefficients in axisTuples)
            {
                if (OrientedBoxCandidate.TryApplyFirstProbe(
                    firstCar, secondCar, scratch, coefficients, out FixedVector? point))
                {
                    result = new OrientedBoxContactCandidate
                    {
                        Routine = OrientedBoxContactCandidateRoutine.AxisCornerFromSecond,
                        Point = point!,
                        Normal = null
                    };

                    return true;
                }
            }

            foreach (int[] coefficients in axisTuples)
            {
                if (OrientedBoxCandidate.TryApplySecondProbe(
                    firstCar, secondCar, scratch, coefficients, out FixedVector? point))
                {
                    result = new OrientedBoxContactCandidate
                    {
                        Routine = OrientedBoxContactCandidateRoutine.AxisCornerFromFirst,
                        Point = point!,
                        Normal = null
                    };

                    return true;
                }
            }

            result = default;

            return false;
        }

        private static bool TryAcceptWithNormal(
            CarMemory firstCar,
            CarMemory secondCar,
            OrientedBoxScratch scratch,
            out OrientedBoxContactCandidate result)
        {
            foreach (int[] coefficients in signTriples)
            {
                if (OrientedBoxCandidate.TryApplyFirstProbeWithNormal(
                    firstCar, secondCar, scratch, coefficients, out FixedVector? point, out FixedVector? normal))
                {
                    result = new OrientedBoxContactCandidate
                    {
                        Routine = OrientedBoxContactCandidateRoutine.SignCornerFromSecond,
                        Point = point!,
                        Normal = normal!
                    };

                    return true;
                }
            }

            foreach (int[] coefficients in signTriples)
            {
                if (OrientedBoxCandidate.TryApplySecondProbeWithNormal(
                    firstCar, secondCar, scratch, coefficients, out FixedVector? point, out FixedVector? normal))
                {
                    result = new OrientedBoxContactCandidate
                    {
                        Routine = OrientedBoxContactCandidateRoutine.SignCornerFromFirst,
                        Point = point!,
                        Normal = normal!
                    };

                    return true;
                }
            }

            result = default;

            return false;
        }
    }
}