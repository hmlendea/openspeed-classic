using System;

namespace OpenSpeed.Classic.Physics
{
    public static class NativeContactCeiling
    {
        private static int RouteIndexOffset => 0x14;

        private static int WorldHeightOffset => 0xA0;

        private static int NormalOffset => 0x124;

        private static int ReferencePointOffset => 0x13C;

        private static int CeilingMarkerOffset => 0x1E;

        private static int CeilingMarker => 2;

        private static int DefaultClearance => 0xA0000;

        private static int VariableClearanceTrack => 4;

        private static int HighClearanceStart => 0x572;

        private static int HighClearanceEnd => 0x5A0;

        private static int HighClearance => 0x180000;

        private static int LowClearanceStart => 0x3EC;

        private static int LowClearanceEnd => 0x42A;

        private static int LowClearance => 0xE0000;

        private static int FixedClearanceTrack => 8;

        private static int FixedClearance => 0x140000;

        private static int AngularLimit => 0x26666;

        public static bool TryResolve(
            CarMemory car,
            PhysicsRoute route,
            int trackIdentifier,
            int planeHeight,
            Action<CarMemory, FixedVector, FixedVector> resolveWorldContact)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(resolveWorldContact);
            int routeIndex = car[RouteIndexOffset];
            int marker = route.ReadWord(routeIndex, CeilingMarkerOffset) & byte.MaxValue;

            if ((marker >> 4) != CeilingMarker &&
                (marker & 0x0F) != CeilingMarker)
            {
                return false;
            }

            int ceilingHeight = unchecked(planeHeight + CalculateClearance(trackIdentifier, routeIndex));

            if (ceilingHeight >= car[WorldHeightOffset])
            {
                return false;
            }

            Resolve(car, resolveWorldContact);

            return true;
        }

        private static int CalculateClearance(int trackIdentifier, int routeIndex)
        {
            if (trackIdentifier == VariableClearanceTrack)
            {
                if (routeIndex > HighClearanceStart && routeIndex < HighClearanceEnd)
                {
                    return HighClearance;
                }

                if (routeIndex > LowClearanceStart && routeIndex < LowClearanceEnd)
                {
                    return LowClearance;
                }
            }

            if (trackIdentifier == FixedClearanceTrack)
            {
                return FixedClearance;
            }

            return DefaultClearance;
        }

        private static void Resolve(
            CarMemory car,
            Action<CarMemory, FixedVector, FixedVector> resolveWorldContact)
        {
            FixedVector normal = FixedVectors.Read(car, NormalOffset);
            normal.First = unchecked(-normal.First);
            normal.Second = unchecked(-normal.Second);
            normal.Third = unchecked(-normal.Third);
            resolveWorldContact(car, normal, FixedVectors.Read(car, ReferencePointOffset));
            car[0xE8] = Math.Clamp(car[0xE8], -AngularLimit, AngularLimit);
            car[0xEC] = Math.Clamp(car[0xEC], -AngularLimit, AngularLimit);
            car[0xF0] = Math.Clamp(car[0xF0], -AngularLimit, AngularLimit);
        }
    }
}