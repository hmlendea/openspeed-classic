using System;

namespace OpenSpeed.Classic.Physics
{
    public static class LateralResponse
    {
        public static void Apply(CarMemory car, PhysicsContext context, bool isAlternate)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(context);

            if ((car.ReadByte(0x1F4) & 0x50) != 0 || car.ReadByte(0x2D8) < 0x0A)
            {
                return;
            }

            FixedVector forward = FixedVectors.Read(car, 0xDC);
            int projection = X86Math.MultiplyQ16(forward.Second, unchecked(-0xA0000));

            if (isAlternate)
            {
                projection = X86Math.TruncatePowerOfTwo(projection, 3);
            }

            int response = X86Math.MultiplyQ16(
                X86Math.MultiplyQ16(projection, car[0x94]),
                car[0x2AC]);

            if (context.Mode != 0 && X86Math.Abs(response) <= context.SteeringScale)
            {
                car[0x2AC] = unchecked(car[0x2AC] + X86Math.TruncatePowerOfTwo(response, 2));
            }
            else
            {
                car[0x2AC] = unchecked(car[0x2AC] + response);
            }
        }
    }
}