using System;

namespace OpenSpeed.Classic.Physics
{
    public static class ContactGeometry
    {
        private static int PositionOffset => 0x9C;

        private static int PrimaryBasisOffset => 0xC4;

        private static int ExtentOffset => 0x108;

        private static int NormalOffset => 0x124;

        private static int ReferencePointOffset => 0x13C;

        private static int MinimumHeightProjection => 0x1999;

        private static int MinimumNormalDivisor => 0x0A;

        private static int UnavailableHeight => unchecked((int)0x83000000);

        public static int CalculateContactHeight(CarMemory car, FixedVector normalOutput)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(normalOutput);

            FixedVector normal = FixedVectors.Read(car, NormalOffset);
            normalOutput.First = normal.First;
            normalOutput.Second = normal.Second;
            normalOutput.Third = normal.Third;

            if (normal.Second <= MinimumHeightProjection)
            {
                return UnavailableHeight;
            }

            return CalculatePlaneHeight(car);
        }

        public static int CalculatePlaneHeight(CarMemory car)
        {
            ArgumentNullException.ThrowIfNull(car);

            FixedVector normal = FixedVectors.Read(car, NormalOffset);
            FixedVector reference = FixedVectors.Read(car, ReferencePointOffset);
            FixedVector relative = FixedVectors.Subtract(FixedVectors.Read(car, PositionOffset), reference);
            int divisor = Math.Max(X86Math.Abs(normal.Second), MinimumNormalDivisor);
            int numerator = unchecked(-X86Math.MultiplyQ16(normal.First, relative.First) -
                X86Math.MultiplyQ16(normal.Third, relative.Third));

            return unchecked(X86Math.DivideQ16(numerator, divisor) + reference.Second);
        }

        public static int CalculateRelativeHeight(CarMemory car, FixedVector normal, FixedVector reference)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(normal);
            ArgumentNullException.ThrowIfNull(reference);

            FixedVector relative = FixedVectors.Subtract(FixedVectors.Read(car, PositionOffset), reference);

            return unchecked(FixedVectors.Dot(normal, relative) - car[ExtentOffset + sizeof(int)]);
        }

        public static int CalculateSupportCorrection(CarMemory car, FixedVector normal, FixedVector reference)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(normal);
            ArgumentNullException.ThrowIfNull(reference);

            FixedVector relative = FixedVectors.Subtract(FixedVectors.Read(car, PositionOffset), reference);
            int correction = FixedVectors.Dot(normal, relative);

            for (int axis = 0; axis < FixedMatrix.Dimension; axis += 1)
            {
                FixedVector row = FixedVectors.Read(car, PrimaryBasisOffset + axis * FixedMatrix.Dimension * sizeof(int));
                int projection = X86Math.MultiplyQ16(FixedVectors.Dot(row, normal), car[ExtentOffset + axis * sizeof(int)]);

                if (projection >= 0)
                {
                    projection = unchecked(-projection);
                }

                correction = unchecked(correction + projection);
            }

            return correction;
        }
    }
}