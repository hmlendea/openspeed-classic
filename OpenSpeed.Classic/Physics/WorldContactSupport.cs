using System;

namespace OpenSpeed.Classic.Physics
{
    public static class WorldContactSupport
    {
        private static int PositionOffset => 0x9C;

        private static int BasisOffset => 0xC4;

        private static int ExtentOffset => 0x108;

        private static int RowSize => FixedMatrix.Dimension * sizeof(int);

        /// <summary>
        /// Selects sub_456420's deepest oriented support corner without changing car state.
        /// Zero projections select the negative extent; signs are applied after Q16 multiplication.
        /// </summary>
        public static FixedVector CalculatePoint(CarMemory car, FixedVector normal)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(normal);
            FixedVector point = FixedVectors.Read(car, PositionOffset);

            for (int axis = 0; axis < FixedMatrix.Dimension; axis += 1)
            {
                FixedVector row = FixedVectors.Read(car, BasisOffset + axis * RowSize);
                int extent = car[ExtentOffset + axis * sizeof(int)];
                int projection = X86Math.MultiplyQ16(FixedVectors.Dot(normal, row), extent);
                FixedVector offset = FixedVectors.Scale(row, extent);

                if (projection >= 0)
                {
                    offset.First = unchecked(-offset.First);
                    offset.Second = unchecked(-offset.Second);
                    offset.Third = unchecked(-offset.Third);
                }

                point = FixedVectors.Add(point, offset);
            }

            return point;
        }
    }
}