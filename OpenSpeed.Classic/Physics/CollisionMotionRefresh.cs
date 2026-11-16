using System;

namespace OpenSpeed.Classic.Physics
{
    public static class CollisionMotionRefresh
    {
        private static int ResponseThreshold => 0x1999;

        private static int AccumulatorThreshold => 0x140000;

        private static int AccumulatorStep => 0x20000;

        private static int AccumulatorMaximum => 0x280000;

        /// <summary>
        /// Executes sub_46A130 using an explicit native route-correction callback.
        /// </summary>
        public static void Update(
            CarMemory car,
            CarSpecifications descriptor,
            int mode,
            Func<CarMemory, int> correctRouteContact)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(correctRouteContact);
            int correction = correctRouteContact(car);

            if (correction == 0)
            {
                return;
            }

            if ((car[0x1F4] & 4) != 0)
            {
                if (mode == 1)
                {
                    RefreshAlternate(car, descriptor);
                }
                else
                {
                    RefreshPrimary(car, mode);
                }
            }

            car[0x284] = correction;
        }

        /// <summary>
        /// Applies sub_46A2E0's longitudinal projection and gear-dependent engine refresh.
        /// </summary>
        public static void RefreshAlternate(CarMemory car, CarSpecifications descriptor)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(descriptor);
            car[0x2B8] = FixedVectors.Dot(FixedVectors.Read(car, 0x1A0), FixedVectors.Read(car, 0xA8));
            int gear = car.ReadByte(0x2DA);

            if (gear != 1)
            {
                int projection = X86Math.MultiplyQ16(car[0x2B8], descriptor[0x168 + gear * sizeof(int)]);
                car[0x2F0] = X86Math.TruncatePowerOfTwo(unchecked(projection * descriptor[0xF0]), 16);
            }

            ClearResponse(car);
        }

        /// <summary>
        /// Applies sub_46A3B0, including the supplied assembly's unreachable engine-update block.
        /// </summary>
        public static void RefreshPrimary(CarMemory car, int mode)
        {
            ArgumentNullException.ThrowIfNull(car);
            FixedVector velocity = FixedVectors.Read(car, 0xA8);
            car[0x2B0] = FixedVectors.Dot(FixedVectors.Read(car, 0x188), velocity);
            car[0x2B8] = FixedVectors.Dot(FixedVectors.Read(car, 0x1A0), velocity);
            ClearResponse(car);

            if (car[0x2B8] <= ResponseThreshold || mode != 1)
            {
                return;
            }

            int ratio = X86Math.Abs(X86Math.DivideQ16(car[0x2B0], car[0x2B8]));

            if (ratio <= ResponseThreshold)
            {
                return;
            }

            int response = -3;

            if (car[0x2B0] < 0)
            {
                response = 3;
            }

            car[0x2F4] = response;
            car[0x310] = X86Math.TruncatePowerOfTwo(car[0x2B0], 2);
            IncreaseAccumulator(car, 0x324);
            IncreaseAccumulator(car, 0x328);
        }

        private static void ClearResponse(CarMemory car)
        {
            car[0x310] = 0;
            car[0x324] = 0;
            car[0x328] = 0;
            car[0x2F4] = 0;
        }

        private static void IncreaseAccumulator(CarMemory car, int offset)
        {
            if (car[offset] < AccumulatorThreshold)
            {
                car[offset] = Math.Min(unchecked(car[offset] + AccumulatorStep), AccumulatorMaximum);
            }
        }
    }
}