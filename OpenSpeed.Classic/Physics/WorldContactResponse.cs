using System;
using System.Numerics;

namespace OpenSpeed.Classic.Physics
{
    public static class WorldContactResponse
    {
        private static int AngularPreScale => 0x6487E;

        private static int AngularPostScale => 0x28BE;

        /// <summary>
        /// Resolves sub_456420 against a supplied world plane using native support geometry.
        /// The position-correction switch corresponds to dword_4D6264.
        /// </summary>
        public static bool ApplyPlane(
            CarMemory car,
            FixedVector normal,
            FixedVector referencePoint,
            bool isPositionCorrectionEnabled)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(normal);
            ArgumentNullException.ThrowIfNull(referencePoint);
            FixedVectors.Write(car, 0xE8, FixedVectors.Scale(FixedVectors.Read(car, 0xE8), AngularPreScale));
            FixedVector contactNormal = CopyNormal(normal);
            int penetration = ContactGeometry.CalculateSupportCorrection(car, contactNormal, referencePoint);
            bool wasApplied = false;

            if (penetration < 0)
            {
                wasApplied = ResolvePenetration(car, contactNormal, penetration, isPositionCorrectionEnabled);
            }

            FinishAngularResponse(car);

            return wasApplied;
        }

        private static bool ResolvePenetration(
            CarMemory car,
            FixedVector normal,
            int penetration,
            bool isPositionCorrectionEnabled)
        {
            FixedVector supportPoint = WorldContactSupport.CalculatePoint(car, normal);
            int depth = Math.Max(unchecked(-penetration), 0);
            FixedVector correction = new();

            if (isPositionCorrectionEnabled && depth != 0)
            {
                correction = FixedVectors.Scale(normal, X86Math.TruncatePowerOfTwo(depth, 1));
                FixedVectors.Write(car, 0x9C, FixedVectors.Add(FixedVectors.Read(car, 0x9C), correction));
            }

            FixedVector offset = FixedVectors.Subtract(supportPoint, FixedVectors.Read(car, 0x9C));
            FixedVector velocity = CalculateContactVelocity(car, offset);
            bool wasApplied = false;

            if (FixedVectors.Dot(normal, velocity) < 0)
            {
                wasApplied = WorldContactImpulse.Apply(car, supportPoint, normal, velocity);
            }

            if (isPositionCorrectionEnabled && depth != 0 && correction.Second > 0)
            {
                AdjustVerticalVelocity(car, correction.Second);
            }

            return wasApplied;
        }

        private static FixedVector CalculateContactVelocity(CarMemory car, FixedVector offset)
            => FixedVectors.Add(
                FixedVectors.Cross(FixedVectors.Read(car, 0xE8), offset), FixedVectors.Read(car, 0xA8));

        private static void AdjustVerticalVelocity(CarMemory car, int correction)
        {
            int reduction = unchecked(X86Math.MultiplyQ16(0x9CCCC, correction) * 2);
            int square = X86Math.MultiplyQ16(car[0xAC], car[0xAC]);

            if (reduction > square)
            {
                car[0xAC] = 0;

                return;
            }

            uint remainder = unchecked((uint)(square - reduction));
            int magnitude = 0;

            if (remainder != 0)
            {
                int index = BitOperations.Log2(remainder);
                magnitude = unchecked((int)((ulong)remainder * (uint)PhysicsTables.MagnitudeMultipliers[index] >> 16) +
                    PhysicsTables.MagnitudeAddends[index]);
            }

            if (car[0xAC] <= 0)
            {
                magnitude = unchecked(-magnitude);
            }

            car[0xAC] = magnitude;
        }

        private static FixedVector CopyNormal(FixedVector normal)
        {
            FixedVector result = new() { First = normal.First, Second = normal.Second, Third = normal.Third };

            if (result.First == 0 && result.Second == 0 && result.Third == 0)
            {
                result.Second = X86Math.One;
            }

            return result;
        }

        private static void FinishAngularResponse(CarMemory car)
        {
            FixedVector angular = FixedVectors.Scale(FixedVectors.Read(car, 0xE8), AngularPostScale);
            angular.First = Math.Clamp(angular.First, -0x1CCCC, 0x1CCCC);
            angular.Second = Math.Clamp(angular.Second, -0x1CCCC, 0x1CCCC);
            angular.Third = Math.Clamp(angular.Third, -0x1CCCC, 0x1CCCC);
            FixedVectors.Write(car, 0xE8, angular);
        }

        public static bool Apply(
            CarMemory car,
            FixedVector supportPoint,
            FixedVector normal,
            FixedVector contactVelocity)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(supportPoint);
            ArgumentNullException.ThrowIfNull(normal);
            ArgumentNullException.ThrowIfNull(contactVelocity);

            FixedVector contactNormal = CopyNormal(normal);

            FixedVectors.Write(car, 0xE8, FixedVectors.Scale(FixedVectors.Read(car, 0xE8), AngularPreScale));
            bool applied = WorldContactImpulse.Apply(car, supportPoint, contactNormal, contactVelocity);
            FinishAngularResponse(car);

            return applied;
        }
    }
}