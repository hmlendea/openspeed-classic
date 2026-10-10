using System.Numerics;

namespace OpenSpeed.Classic.Physics
{
    public static class FixedVectors
    {
        private static int NormalisationComponentLimit => 0x1000000;

        private static uint NormalisationSumLimit => 0x40000000;

        public static FixedVector Read(CarMemory memory, int offset) => new()
        {
            First = memory[offset],
            Second = memory[offset + sizeof(int)],
            Third = memory[offset + sizeof(int) * 2]
        };

        public static void Write(CarMemory memory, int offset, FixedVector vector)
        {
            memory[offset] = vector.First;
            memory[offset + sizeof(int)] = vector.Second;
            memory[offset + sizeof(int) * 2] = vector.Third;
        }

        public static FixedVector Add(FixedVector left, FixedVector right) => new()
        {
            First = unchecked(left.First + right.First),
            Second = unchecked(left.Second + right.Second),
            Third = unchecked(left.Third + right.Third)
        };

        public static FixedVector Subtract(FixedVector left, FixedVector right) => new()
        {
            First = unchecked(left.First - right.First),
            Second = unchecked(left.Second - right.Second),
            Third = unchecked(left.Third - right.Third)
        };

        public static FixedVector Scale(FixedVector vector, int scale) => new()
        {
            First = X86Math.MultiplyQ16(vector.First, scale),
            Second = X86Math.MultiplyQ16(vector.Second, scale),
            Third = X86Math.MultiplyQ16(vector.Third, scale)
        };

        public static int Dot(FixedVector left, FixedVector right) => unchecked(
            X86Math.MultiplyQ16(left.First, right.First) +
            X86Math.MultiplyQ16(left.Second, right.Second) +
            X86Math.MultiplyQ16(left.Third, right.Third));

        public static FixedVector Cross(FixedVector left, FixedVector right) => new()
        {
            First = unchecked(X86Math.MultiplyQ16(left.Second, right.Third) - X86Math.MultiplyQ16(left.Third, right.Second)),
            Second = unchecked(X86Math.MultiplyQ16(left.Third, right.First) - X86Math.MultiplyQ16(left.First, right.Third)),
            Third = unchecked(X86Math.MultiplyQ16(left.First, right.Second) - X86Math.MultiplyQ16(left.Second, right.First))
        };

        public static int Normalise(FixedVector vector)
        {
            int first = vector.First;
            int second = vector.Second;
            int third = vector.Third;

            while (X86Math.Abs(first) > NormalisationComponentLimit ||
                X86Math.Abs(second) > NormalisationComponentLimit ||
                X86Math.Abs(third) > NormalisationComponentLimit)
            {
                first >>= 1;
                second >>= 1;
                third >>= 1;
            }

            int firstSquared;
            int secondSquared;
            int thirdSquared;

            while (true)
            {
                firstSquared = X86Math.MultiplyQ16(first, first);
                secondSquared = X86Math.MultiplyQ16(second, second);
                thirdSquared = X86Math.MultiplyQ16(third, third);
                uint quarterSum = unchecked(((uint)firstSquared >> 2) +
                    ((uint)secondSquared >> 2) + ((uint)thirdSquared >> 2));

                if (quarterSum <= NormalisationSumLimit)
                {
                    break;
                }

                first >>= 1;
                second >>= 1;
                third >>= 1;
            }

            uint sum = unchecked((uint)(firstSquared + secondSquared + thirdSquared));

            if (sum == 0)
            {
                return 0;
            }

            int index = BitOperations.Log2(sum);
            int magnitude = unchecked((int)((ulong)sum * (uint)PhysicsTables.MagnitudeMultipliers[index] >> 16) +
                PhysicsTables.MagnitudeAddends[index]);
            vector.First = X86Math.DivideQ16(first, magnitude);
            vector.Second = X86Math.DivideQ16(second, magnitude);
            vector.Third = X86Math.DivideQ16(third, magnitude);

            return vector.Third;
        }

        public static void NormaliseBasisVector(FixedVector vector)
        {
            uint sum = unchecked((uint)Dot(vector, vector));

            if (sum == 0)
            {
                return;
            }

            int index = BitOperations.Log2(sum);
            int magnitude = unchecked((int)((ulong)sum * (uint)PhysicsTables.MagnitudeMultipliers[index] >> 16) +
                PhysicsTables.MagnitudeAddends[index]);

            if (magnitude == 0)
            {
                return;
            }

            int inverse = X86Math.DivideQ16(X86Math.One, magnitude);
            vector.First = X86Math.MultiplyQ16(vector.First, inverse);
            vector.Second = X86Math.MultiplyQ16(vector.Second, inverse);
            vector.Third = X86Math.MultiplyQ16(vector.Third, inverse);
        }
    }
}