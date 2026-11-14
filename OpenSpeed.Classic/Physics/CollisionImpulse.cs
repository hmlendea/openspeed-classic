namespace OpenSpeed.Classic.Physics
{
    public static class CollisionImpulse
    {
        private static int PairResponseScale => 0x8CCC;

        private static int PairEventCode => 0x50001;

        public static bool IntersectsBroadPhase(CarMemory first, CarMemory second)
        {
            int radius = unchecked(first[0x114] + second[0x114]);

            return X86Math.Abs(unchecked(first[0xA4] - second[0xA4])) < radius &&
                X86Math.Abs(unchecked(first[0x9C] - second[0x9C])) < radius &&
                X86Math.Abs(unchecked(first[0xA0] - second[0xA0])) < radius;
        }

        public static bool ContainsWorldPoint(CarMemory car, FixedVector point, int radius)
        {
            FixedVector delta = FixedVectors.Subtract(point, FixedVectors.Read(car, 0x9C));
            delta.Second = 0;
            int first = FixedVectors.Dot(FixedVectors.Read(car, 0xC4), delta);
            int third = FixedVectors.Dot(FixedVectors.Read(car, 0xDC), delta);

            return X86Math.Abs(first) <= unchecked(car[0x108] + radius) &&
                X86Math.Abs(third) <= unchecked(car[0x110] + radius);
        }

        public static bool ApplyPair(CarMemory first, CarMemory second, FixedVector point, FixedVector normal, int firstAddress, int secondAddress)
        {
            FixedVector firstLever = FixedVectors.Subtract(point, FixedVectors.Read(first, 0x9C));
            FixedVector secondLever = FixedVectors.Subtract(point, FixedVectors.Read(second, 0x9C));
            FixedVector firstCross = FixedVectors.Cross(firstLever, normal);
            FixedVector secondCross = FixedVectors.Cross(secondLever, normal);
            int firstVelocityProjection = FixedVectors.Dot(normal, FixedVectors.Read(first, 0xA8));
            int secondVelocityProjection = FixedVectors.Dot(normal, FixedVectors.Read(second, 0xA8));
            int relative = unchecked(secondVelocityProjection - firstVelocityProjection);
            relative = unchecked(relative - FixedVectors.Dot(firstCross, FixedVectors.Read(first, 0xE8)));
            relative = unchecked(relative + FixedVectors.Dot(secondCross, FixedVectors.Read(second, 0xE8)));
            int denominator = unchecked(X86Math.TruncatePowerOfTwo(first[0xB8], 1) + X86Math.TruncatePowerOfTwo(second[0xB8], 1));
            denominator = unchecked(denominator + X86Math.TruncatePowerOfTwo(
                X86Math.MultiplyQ16(first[0xF8], FixedVectors.Dot(firstCross, firstCross)), 1));
            denominator = unchecked(denominator + X86Math.TruncatePowerOfTwo(
                X86Math.MultiplyQ16(second[0xF8], FixedVectors.Dot(secondCross, secondCross)), 1));
            int response = X86Math.DivideQ16(relative, denominator);

            if (response < 0)
            {
                return false;
            }

            int magnitude = X86Math.MultiplyQ16(response, PairResponseScale);
            FixedVector impulse = FixedVectors.Scale(normal, magnitude);
            ApplyBodyImpulse(first, firstLever, impulse, false);
            ApplyBodyImpulse(second, secondLever, impulse, true);
            int metric = X86Math.MultiplyQ16(unchecked(firstVelocityProjection - secondVelocityProjection), unchecked(first[0xB4] + second[0xB4]));
            FixedVector midpoint = FixedVectors.Add(FixedVectors.Read(first, 0x9C), FixedVectors.Read(second, 0x9C));
            midpoint.First = X86Math.TruncatePowerOfTwo(midpoint.First, 1);
            midpoint.Second = X86Math.TruncatePowerOfTwo(midpoint.Second, 1);
            midpoint.Third = X86Math.TruncatePowerOfTwo(midpoint.Third, 1);
            StoreEvent(first, metric, secondAddress, midpoint);
            StoreEvent(second, metric, firstAddress, midpoint);

            return true;
        }

        private static void ApplyBodyImpulse(CarMemory car, FixedVector lever, FixedVector impulse, bool isSubtracted)
        {
            FixedVector linear = FixedVectors.Scale(impulse, car[0xB8]);
            FixedVector angular = FixedVectors.Scale(FixedVectors.Cross(lever, impulse), car[0xF8]);
            FixedVector velocity = FixedVectors.Read(car, 0xA8);
            FixedVector rotation = FixedVectors.Read(car, 0xE8);

            if (isSubtracted)
            {
                velocity = FixedVectors.Subtract(velocity, linear);
                rotation = FixedVectors.Subtract(rotation, angular);
            }
            else
            {
                velocity = FixedVectors.Add(velocity, linear);
                rotation = FixedVectors.Add(rotation, angular);
            }

            FixedVectors.Write(car, 0xA8, velocity);
            FixedVectors.Write(car, 0xE8, rotation);
        }

        private static void StoreEvent(CarMemory car, int metric, int partnerAddress, FixedVector point)
        {
            car[0x160] = X86Math.Abs(X86Math.MultiplyQ16(metric, car[0xB8]));
            car[0x164] = partnerAddress;
            car[0x168] = PairEventCode;
            FixedVectors.Write(car, 0x170, point);
            car.WriteWord(0x14C, unchecked((ushort)(car.ReadWord(0x14C) + 1)));
        }
    }
}