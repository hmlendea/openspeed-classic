using System;

namespace OpenSpeed.Classic.Physics
{
    public static class ContactBasis
    {
        private static int PrimaryOffset => 0xC4;

        private static int SecondaryOffset => 0x188;

        private static int RowSize => FixedMatrix.Dimension * sizeof(int);

        /// <summary>
        /// Constructs sub_461960's secondary basis without normalising the supplied normal.
        /// The low-projection branch compares the raw integer dot magnitude against x87 value 0.5.
        /// </summary>
        public static void Update(CarMemory car, FixedVector normal, int projection)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(normal);

            if ((car[0x1F4] & 4) == 0 && projection > 0xE666)
            {
                FixedMatrices.Write(car, SecondaryOffset, FixedMatrices.Read(car, PrimaryOffset));
                FixedVectors.Write(car, SecondaryOffset + RowSize, normal);

                return;
            }

            FixedVectors.Write(car, SecondaryOffset + RowSize, normal);
            FixedVector first = FixedVectors.Read(car, PrimaryOffset);
            int dot = X86Math.Abs(FixedVectors.Dot(normal, first));

            if (projection >= 0x8000 || dot <= 0)
            {
                FixedVector third = FixedVectors.Cross(first, normal);
                FixedVectors.NormaliseBasisVector(third);
                FixedVectors.Write(car, SecondaryOffset + RowSize * 2, third);
                first = FixedVectors.Cross(normal, third);
                FixedVectors.NormaliseBasisVector(first);
                FixedVectors.Write(car, SecondaryOffset, first);

                return;
            }

            first = FixedVectors.Cross(normal, FixedVectors.Read(car, PrimaryOffset + RowSize * 2));
            FixedVectors.NormaliseBasisVector(first);
            FixedVectors.Write(car, SecondaryOffset, first);
            FixedVector finalThird = FixedVectors.Cross(first, normal);
            FixedVectors.NormaliseBasisVector(finalThird);
            FixedVectors.Write(car, SecondaryOffset + RowSize * 2, finalThird);
        }
    }
}