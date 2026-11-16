using System;

namespace OpenSpeed.Classic.Physics
{
    public static class AiMotion
    {
        private static int SelectedTargetOffset => 0x394;

        private static int CurrentLateralOffset => 0x3AC;

        private static int MotionTargetOffset => 0x548;

        private static int LateralVelocityOffset => 0x3CC;

        private static int TargetStep => 0x3333;

        public static void SmoothLateralTarget(CarMemory car)
        {
            ArgumentNullException.ThrowIfNull(car);

            int current = car[CurrentLateralOffset];
            int target = car[SelectedTargetOffset];
            int motion = car[MotionTargetOffset];

            if (current > target && current < motion || current > motion && current < target)
            {
                motion = current;
            }

            if (motion < target)
            {
                motion = Math.Min(unchecked(motion + TargetStep), target);
            }
            else if (motion > target)
            {
                motion = Math.Max(unchecked(motion - TargetStep), target);
            }

            car[MotionTargetOffset] = motion;
        }

        public static void CalculateLateralVelocity(CarMemory car)
        {
            ArgumentNullException.ThrowIfNull(car);

            car[LateralVelocityOffset] = unchecked((car[MotionTargetOffset] - car[CurrentLateralOffset]) << 3);
        }

        public static void ComposeWorldVelocity(CarMemory car)
        {
            ArgumentNullException.ThrowIfNull(car);

            int lateral = car[LateralVelocityOffset];
            int speed = X86Math.Abs(car[0x39C]);
            FixedVector right = FixedVectors.Read(car, 0x118);
            FixedVector forward = FixedVectors.Read(car, 0xDC);
            FixedVectors.Write(car, 0xA8, FixedVectors.Add(
                FixedVectors.Scale(right, lateral),
                FixedVectors.Scale(forward, speed)));
            FixedVectors.Write(car, 0xE8, new FixedVector());
            car[0x2B8] = car[0x3A0];
        }
    }
}