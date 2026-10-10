using System;

namespace OpenSpeed.Classic.Physics
{
    public static class RouteBasis
    {
        private static int PrimaryBasisOffset => 0xC4;

        private static int SecondaryBasisOffset => 0x188;

        private static int RouteBasisOffset => 0x118;

        private static int ReferencePointOffset => 0x13C;

        private static int HeadingOffset => 0x148;

        private static int RightRecordOffset => 0x12;

        private static int NormalRecordOffset => 0x0C;

        private static int ForwardRecordOffset => 0x0F;

        public static void LoadRecord(CarMemory car, PhysicsRoute route, int index)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(route);

            FixedVectors.Write(car, ReferencePointOffset, route.Position(index));
            FixedVectors.Write(car, RouteBasisOffset, route.Direction(index, RightRecordOffset));
            FixedVectors.Write(car, RouteBasisOffset + FixedMatrix.Dimension * sizeof(int), route.Direction(index, NormalRecordOffset));
            FixedVectors.Write(car, RouteBasisOffset + FixedMatrix.Dimension * sizeof(int) * 2, route.Direction(index, ForwardRecordOffset));
            car[HeadingOffset] = route.Heading(index);
        }

        public static void CopyPrimary(CarMemory car, bool isReversed)
            => CopyBasis(car, PrimaryBasisOffset, isReversed);

        public static void CopySecondary(CarMemory car, bool isReversed)
            => CopyBasis(car, SecondaryBasisOffset, isReversed);

        private static void CopyBasis(CarMemory car, int destination, bool isReversed)
        {
            ArgumentNullException.ThrowIfNull(car);

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    int displacement = (row * FixedMatrix.Dimension + column) * sizeof(int);
                    int value = car[RouteBasisOffset + displacement];

                    if (isReversed && row != 1)
                    {
                        value = unchecked(-value);
                    }

                    car[destination + displacement] = value;
                }
            }
        }
    }
}