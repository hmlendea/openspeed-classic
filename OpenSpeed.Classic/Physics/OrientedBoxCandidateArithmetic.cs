using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Shared arithmetic for the oriented-box candidate helpers <c>sub_457CD0</c> and
    /// <c>sub_457A10</c>. It provides the probe projection, the inclusive extent test,
    /// the extreme-corner construction, and the signed world normal.
    /// </summary>
    internal static class OrientedBoxCandidateArithmetic
    {
        public static int GetComponent(FixedVector vector, int component)
            => component switch
            {
                0 => vector.First,
                1 => vector.Second,
                _ => vector.Third
            };

        public static void SetComponent(ref FixedVector vector, int component, int value)
        {
            switch (component)
            {
                case 0:
                    vector.First = value;

                    break;
                case 1:
                    vector.Second = value;

                    break;
                default:
                    vector.Third = value;

                    break;
            }
        }

        public static void AddComponent(ref FixedVector vector, int component, int value)
            => SetComponent(ref vector, component, unchecked(GetComponent(vector, component) + value));

        public static int[] CalculateProbe(
            int[] deltaInFrame,
            int[] extentProjections,
            ReadOnlySpan<int> coefficients)
        {
            int[] probe = new int[FixedMatrix.Dimension];

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                int total = deltaInFrame[row];

                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    total = unchecked(total +
                        extentProjections[row * FixedMatrix.Dimension + column] * coefficients[column]);
                }

                probe[row] = total;
            }

            return probe;
        }

        public static bool IsInsideExtents(
            CarMemory car,
            ReadOnlySpan<int> probe,
            int extentOffset)
        {
            for (int axis = 0; axis < FixedMatrix.Dimension; axis += 1)
            {
                int extent = car[extentOffset + axis * sizeof(int)];

                if (probe[axis] > extent || unchecked(-extent) > probe[axis])
                {
                    return false;
                }
            }

            return true;
        }

        public static FixedVector CalculateCorner(
            CarMemory car,
            ReadOnlySpan<int> coefficients,
            int positionOffset,
            int extentOffset)
        {
            FixedVector corner = FixedVectors.Read(car, positionOffset);

            for (int axis = 0; axis < FixedMatrix.Dimension; axis += 1)
            {
                int extent = car[extentOffset + axis * sizeof(int)];

                for (int component = 0; component < FixedMatrix.Dimension; component += 1)
                {
                    int basis = car[0xC4 + (axis * FixedMatrix.Dimension + component) * sizeof(int)];
                    int projected = X86Math.MultiplyQ16(basis, extent) * coefficients[axis];
                    AddComponent(ref corner, component, projected);
                }
            }

            return corner;
        }

        public static FixedVector CalculateNormal(
            CarMemory car,
            ReadOnlySpan<int> coefficients,
            int sign)
        {
            FixedVector normal = new();

            for (int component = 0; component < FixedMatrix.Dimension; component += 1)
            {
                int total = 0;

                for (int axis = 0; axis < FixedMatrix.Dimension; axis += 1)
                {
                    int basis = car[0xC4 + (axis * FixedMatrix.Dimension + component) * sizeof(int)];
                    total = unchecked(total + basis * coefficients[axis]);
                }

                SetComponent(ref normal, component, unchecked(total * sign));
            }

            return normal;
        }
    }
}