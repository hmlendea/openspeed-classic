namespace OpenSpeed.Classic.Physics
{
    public static class SurfaceResponse
    {
        public static void Prepare(CarMemory car, CarSpecifications descriptor, CarRuntimeType runtimeType, PhysicsContext context, PhysicsRoute route)
        {
            int current = car[0x14];
            int previous = current - 1;

            if (previous < 0)
            {
                previous += route.Count;
            }

            FixedVector currentForward = route.Direction(current, 0x0F);
            FixedVector previousNormal = route.Direction(previous, 0x0C);
            FixedVector previousForward = route.Direction(previous, 0x0F);
            int angle = PhysicsAngles.RatioAngle(
                FixedVectors.Dot(previousNormal, currentForward),
                FixedVectors.Dot(previousForward, currentForward));
            int speedFactor = X86Math.MultiplyQ16(X86Math.Abs(car[0x2B8]), 0xA3D);

            if (speedFactor > 0x20000)
            {
                speedFactor = 0x20000;
            }

            int angleFactor = X86Math.MultiplyQ16(unchecked((angle + angle) << 16), 0x28F);
            context.FrontCoefficient = X86Math.One;
            context.RearCoefficient = X86Math.One;
            context.SurfaceCoefficient = unchecked(PhysicsTables.SurfaceCoefficients[context.Mode * 10 + car[0x200]] +
                X86Math.MultiplyQ16(speedFactor, angleFactor));

            if (car[0x2B8] <= 0x50000)
            {
                return;
            }

            int front = X86Math.MultiplyQ16(descriptor[0x13C], runtimeType[0x1C]);
            context.FrontCoefficient = unchecked(X86Math.One + X86Math.MultiplyQ16(car[0x2B8], front));
            int rear = X86Math.MultiplyQ16(descriptor[0x13C], runtimeType[0x20]);

            if ((runtimeType[0] == 0 || runtimeType[0] == 0x0E) && car[0x2B8] > 0x240000 && car.ReadByte(0x2D8) > 0)
            {
                rear = unchecked(rear + rear);
            }
            else
            {
                rear = X86Math.TruncatePowerOfTwo(unchecked(rear * 3), 1);
            }

            context.RearCoefficient = unchecked(X86Math.One + X86Math.MultiplyQ16(car[0x2B8], rear));
        }

        public static void ApplyGravityResponse(CarMemory car, PhysicsContext context, bool isAlternate)
        {
            if ((car.ReadByte(0x1F4) & 0x50) != 0 || car.ReadByte(0x2D8) >= 0x0A)
            {
                return;
            }

            int gravity = X86Math.MultiplyQ16(-0xA0000, car[0xE0]);

            if (isAlternate)
            {
                gravity = X86Math.TruncatePowerOfTwo(gravity, 3);
            }

            gravity = X86Math.MultiplyQ16(gravity, car[0x94]);
            int projection = X86Math.MultiplyQ16(car[0x2AC], gravity);

            if (projection > context.SteeringScale || context.Mode == 0)
            {
                car[0x2AC] = unchecked(car[0x2AC] + gravity);
            }
            else if (projection < unchecked(-context.SteeringScale))
            {
                car[0x2AC] = unchecked(car[0x2AC] + X86Math.TruncatePowerOfTwo(gravity, 2));
            }
        }
    }
}