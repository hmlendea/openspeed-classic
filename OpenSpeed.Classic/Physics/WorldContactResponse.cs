using System;

namespace OpenSpeed.Classic.Physics
{
    public static class WorldContactResponse
    {
        private static int AngularPreScale => 0x6487E;

        private static int AngularPostScale => 0x28BE;

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

            FixedVector contactNormal = new()
            {
                First = normal.First,
                Second = normal.Second,
                Third = normal.Third
            };

            if (contactNormal.First == 0 && contactNormal.Second == 0 && contactNormal.Third == 0)
            {
                contactNormal.Second = X86Math.One;
            }

            FixedVectors.Write(car, 0xE8, FixedVectors.Scale(FixedVectors.Read(car, 0xE8), AngularPreScale));
            bool applied = WorldContactImpulse.Apply(car, supportPoint, contactNormal, contactVelocity);
            FixedVector angular = FixedVectors.Scale(FixedVectors.Read(car, 0xE8), AngularPostScale);
            angular.First = Math.Clamp(angular.First, -0x1CCCC, 0x1CCCC);
            angular.Second = Math.Clamp(angular.Second, -0x1CCCC, 0x1CCCC);
            angular.Third = Math.Clamp(angular.Third, -0x1CCCC, 0x1CCCC);
            FixedVectors.Write(car, 0xE8, angular);

            return applied;
        }
    }
}