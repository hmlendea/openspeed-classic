using System;
using System.Numerics;

namespace OpenSpeed.Classic.Physics
{
    public static class WorldContactImpulse
    {
        private static int ResponseScale => 0x6666;

        private static int CapacityScale => 0xCCCC;

        private static int VerticalEventThreshold => 0xB333;

        public static bool Apply(CarMemory car, FixedVector supportPoint, FixedVector normal, FixedVector contactVelocity)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(supportPoint);
            ArgumentNullException.ThrowIfNull(normal);
            ArgumentNullException.ThrowIfNull(contactVelocity);

            FixedVector offset = FixedVectors.Subtract(supportPoint, FixedVectors.Read(car, 0x9C));
            FixedVector lever = FixedVectors.Cross(offset, normal);
            int linearProjection = FixedVectors.Dot(normal, FixedVectors.Read(car, 0xA8));
            int angularProjection = FixedVectors.Dot(FixedVectors.Read(car, 0xE8), lever);
            int angularScale = unchecked(car[0xF8] + car[0xF8]);
            int denominator = unchecked(X86Math.TruncatePowerOfTwo(X86Math.TruncatePowerOfTwo(car[0xB8], 1), 1) +
                X86Math.TruncatePowerOfTwo(X86Math.MultiplyQ16(SquaredMagnitude(lever), angularScale), 1));
            int impulse = X86Math.MultiplyQ16(
                X86Math.DivideQ16(unchecked(-(linearProjection + angularProjection)), denominator), ResponseScale);
            StoreEvent(car, supportPoint, normal, linearProjection);

            FixedVector tangent = FixedVectors.Subtract(contactVelocity,
                FixedVectors.Scale(normal, FixedVectors.Dot(contactVelocity, normal)));
            int limit = ApproximateMagnitude(tangent);
            int halfLimit = X86Math.TruncatePowerOfTwo(limit, 1);
            int inverse = -X86Math.One;

            if (halfLimit != 0)
            {
                inverse = unchecked(-X86Math.DivideQ16(0x8000, halfLimit));
            }

            tangent = FixedVectors.Scale(tangent, inverse);
            int capacity = X86Math.MultiplyQ16(CapacityScale, impulse);
            FixedVector candidate = CalculateCapacityCandidate(car, offset, tangent, capacity);
            int candidateProjection = FixedVectors.Dot(candidate, tangent);

            if (candidateProjection > limit)
            {
                capacity = X86Math.MultiplyQ16(capacity, X86Math.DivideQ16(limit, candidateProjection));
            }

            FixedVector friction = FixedVectors.Scale(tangent, capacity);

            if (impulse <= 0)
            {
                return false;
            }

            FixedVector combined = FixedVectors.Add(FixedVectors.Scale(normal, impulse), friction);
            FixedVector linearDelta = FixedVectors.Scale(combined, X86Math.TruncatePowerOfTwo(car[0xB8], 1));
            FixedVector angularDelta = FixedVectors.Scale(FixedVectors.Cross(offset, combined), angularScale);
            car[0xA8] = unchecked(car[0xA8] + linearDelta.First);
            car[0xB0] = unchecked(car[0xB0] + linearDelta.Third);
            car[0xAC] = unchecked(car[0xAC] + linearDelta.Second);
            car[0xE8] = unchecked(car[0xE8] + angularDelta.First);
            car[0xEC] = unchecked(car[0xEC] + angularDelta.Second);
            car[0xF0] = unchecked(car[0xF0] + angularDelta.Third);

            return true;
        }

        public static FixedVector CalculateCapacityCandidate(CarMemory car, FixedVector offset, FixedVector tangent, int capacity)
        {
            int linearScale = X86Math.MultiplyQ16(capacity, X86Math.TruncatePowerOfTwo(car[0xB8], 1));
            FixedVector linear = FixedVectors.Scale(tangent, linearScale);
            int angularScale = X86Math.MultiplyQ16(capacity, unchecked(car[0xF8] + car[0xF8]));
            FixedVector coupled = FixedVectors.Cross(tangent, offset);
            coupled.First = unchecked(X86Math.MultiplyQ16(coupled.Second, offset.Third) -
                X86Math.MultiplyQ16(coupled.Third, offset.Second));
            coupled.Second = unchecked(X86Math.MultiplyQ16(coupled.Third, offset.First) -
                X86Math.MultiplyQ16(coupled.First, offset.Third));
            coupled.Third = unchecked(X86Math.MultiplyQ16(coupled.First, offset.Second) -
                X86Math.MultiplyQ16(coupled.Second, offset.First));

            return FixedVectors.Add(linear, FixedVectors.Scale(coupled, angularScale));
        }

        public static int SquaredMagnitude(FixedVector vector)
        {
            int first = vector.First;
            int second = vector.Second;
            int third = vector.Third;
            int reductions = 0;

            while (X86Math.Abs(first) > 0x1000000 || X86Math.Abs(second) > 0x1000000 || X86Math.Abs(third) > 0x1000000)
            {
                first >>= 1;
                second >>= 1;
                third >>= 1;
                reductions += 1;
            }

            int firstSquared;
            int secondSquared;
            int thirdSquared;

            while (true)
            {
                firstSquared = X86Math.MultiplyQ16(first, first);
                secondSquared = X86Math.MultiplyQ16(second, second);
                thirdSquared = X86Math.MultiplyQ16(third, third);
                uint quarterSum = unchecked(((uint)firstSquared >> 2) + ((uint)secondSquared >> 2) + ((uint)thirdSquared >> 2));

                if (quarterSum <= 0x40000000)
                {
                    break;
                }

                first >>= 1;
                second >>= 1;
                third >>= 1;
                reductions += 1;
            }

            int sum = unchecked(firstSquared + secondSquared + thirdSquared);

            for (int reduction = 0; reduction < reductions; reduction += 1)
            {
                sum = unchecked(sum << 2);
            }

            return sum;
        }

        private static int ApproximateMagnitude(FixedVector vector)
        {
            uint sum = unchecked((uint)FixedVectors.Dot(vector, vector));

            if (sum == 0)
            {
                return 0;
            }

            int index = BitOperations.Log2(sum);

            return unchecked((int)((ulong)sum * (uint)PhysicsTables.MagnitudeMultipliers[index] >> 16) +
                PhysicsTables.MagnitudeAddends[index]);
        }

        private static void StoreEvent(CarMemory car, FixedVector supportPoint, FixedVector normal, int projection)
        {
            int multiplier = 4;

            if (normal.Second < VerticalEventThreshold)
            {
                multiplier = 6;
            }

            car[0x160] = unchecked(X86Math.Abs(projection) * multiplier);
            car[0x164] = 0;
            car[0x168] = car[0x184] | 0x30000;
            FixedVectors.Write(car, 0x170, supportPoint);
        }
    }
}