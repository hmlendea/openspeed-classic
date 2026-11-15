using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates <c>sub_465840</c>: the demand/capacity guard pass of the alternate contact
    /// pipeline. It normalises the signed response state <c>+0x2F4</c>, derives a demand scalar
    /// from grip and contact force, advances the persistent contact accumulators
    /// <c>+0x324</c>, <c>+0x328</c>, and <c>+0x364</c>, and rewrites <c>+0x30C</c> and
    /// <c>+0x310</c>.
    /// </summary>
    /// <remarks>
    /// The routine copies the twelve dwords at <c>dword_4646D0</c> onto its stack, so its two
    /// six-dword halves are <see cref="TyreResponseTables.CapacityStates"/> entries 0..5 and
    /// 6..11. Register reuse between the mutually exclusive paths is preserved by explicit
    /// local state rather than merged arithmetic.
    /// </remarks>
    public static class ContactDemandGuard
    {
        private static int GripOffset => 0x304;

        private static int ContactForceOffset => 0x30C;

        private static int NormalisedForceOffset => 0x310;

        private static int ResponseStateOffset => 0x2F4;

        private static int EngineSpeedOffset => 0x2F0;

        private static int PersistentFirstOffset => 0x324;

        private static int PersistentSecondOffset => 0x328;

        private static int StorageOffset => 0x364;

        private static int DemandCoefficientOffset => 0x158;

        private static int EdgeClampCoefficientOffset => 0x148;

        private static int SurfaceOffset => 0x200;

        private static int AcceleratorByteOffset => 0x2D7;

        private static int ContactEventOffset => 0x284;

        private static int ProjectedNormalOffset => 0x2B8;

        private static int HighGripThreshold => 0xF0000;

        private static int EngineMinimum => 0xBB8;

        private static int GripLocalisationThreshold => 0x50000;

        private static int PersistentLimit => 0x50000;

        private static int AccumulatorStep => 0x3333;

        private static int AccumulatorLimit => 0x280000;

        private static int AccumulatorStart => 0x140000;

        private static int AccumulatorIncrement => 0x20000;

        private static int BrakeForceThreshold => 0x51E;

        private static int SteeringDeltaLimit => 0x4CCC;

        private static int NormalisedForceLimit => 0x3333;

        private static int TableHighBase => 6;

        /// <summary>
        /// Runs the demand/capacity guard pass. The source enters its state-normalisation block
        /// only when the runtime state record selects the contact-alive value <c>0x0E</c>.
        /// </summary>
        public static void Update(
            CarMemory car,
            CarSpecifications descriptor,
            PhysicsContext context,
            bool isContactAlive)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(context);

            int computed = 0;
            int track = 0;
            bool brakeFlag = false;

            if (isContactAlive && X86Math.Abs(car[ResponseStateOffset]) is 5 or 1 or 4)
            {
                car[ResponseStateOffset] = 0;
            }

            // High-grip release: reached only while the response state is exactly three.
            if (X86Math.Abs(car[ResponseStateOffset]) == 3 &&
                (X86Math.Abs(car[GripOffset]) < HighGripThreshold || car[EngineSpeedOffset] < EngineMinimum) &&
                X86Math.Abs(car[ResponseStateOffset]) != 5)
            {
                car[ResponseStateOffset] = 0;
            }

            if (car[ResponseStateOffset] == 0 || car[ResponseStateOffset] == 5)
            {
                int truncatedGrip = X86Math.TruncatePowerOfTwo(car[GripOffset], 16);
                int truncatedForce = X86Math.TruncatePowerOfTwo(car[ContactForceOffset], 9);
                int demand = unchecked(truncatedForce * truncatedGrip * truncatedGrip);

                if (demand <= 0)
                {
                    demand = X86Math.Negate(demand);
                }

                track = demand;
                car[ResponseStateOffset] = 0;

                if (car.ReadByte(AcceleratorByteOffset) < 0x40)
                {
                    int difference = unchecked(demand - descriptor[DemandCoefficientOffset]);
                    int remainder = difference < 0 ? 0 : difference;
                    track = unchecked(X86Math.One - remainder);

                    if (track < 0x9999)
                    {
                        car[PersistentFirstOffset] = Math.Min(
                            unchecked(car[PersistentFirstOffset] + AccumulatorStep), PersistentLimit);
                        car[ResponseStateOffset] = 5;
                        car[StorageOffset] = X86Math.TruncatePowerOfTwo(car[StorageOffset], 1);
                    }

                    if (track < 0x8000)
                    {
                        car[PersistentSecondOffset] = Math.Min(
                            unchecked(car[PersistentSecondOffset] + AccumulatorStep), PersistentLimit);
                    }
                }
            }

            int selector = ReadLow(X86Math.Abs(car[ResponseStateOffset]));

            if ((X86Math.Abs(car[GripOffset]) < GripLocalisationThreshold &&
                 car.ReadByte(AcceleratorByteOffset) < 0x20) ||
                car[ResponseStateOffset] == 0 ||
                car.ReadByte(AcceleratorByteOffset) == 0)
            {
                selector = unchecked(selector + selector);
            }

            bool entersBrakePath =
                car[ContactForceOffset] > unchecked(-BrakeForceThreshold) && car[ResponseStateOffset] == -3 ||
                car[ContactForceOffset] < BrakeForceThreshold && car[ResponseStateOffset] == 3;

            if (entersBrakePath)
            {
                brakeFlag = true;

                if (car[SurfaceOffset] < 3)
                {
                    int tableValue = ReadLow(X86Math.Abs(car[ResponseStateOffset]));

                    if (context.Accelerator > 0xC000)
                    {
                        selector = Math.Min(unchecked(selector * 3), unchecked(tableValue * 3));
                    }
                    else
                    {
                        selector = Math.Min(unchecked(selector * 4), unchecked(tableValue * 4));
                    }
                }

                bool clearsBrakeState =
                    car[ResponseStateOffset] == -3 && car[NormalisedForceOffset] < SteeringDeltaLimit ||
                    car[ResponseStateOffset] == 3 && car[NormalisedForceOffset] > unchecked(-SteeringDeltaLimit);

                if (clearsBrakeState)
                {
                    car[ResponseStateOffset] = 0;
                    car[NormalisedForceOffset] = 0;
                }
                else
                {
                    car[ContactForceOffset] = 0;
                }
            }

            // The negative-state branch builds the signed absorption term by walking the second
            // six-dword half of the table with the negated state.
            int negativeMagnitude = unchecked(-car[ResponseStateOffset]);

            if (negativeMagnitude >= 2 && negativeMagnitude != 5 && negativeMagnitude < TableHighBase + TyreResponseTables.CapacityStates.Length)
            {
                int high = ReadLow(TableHighBase + negativeMagnitude);
                int term = X86Math.MultiplyQ16(high, descriptor[EdgeClampCoefficientOffset]);
                term = X86Math.MultiplyQ16(term, car[ContactForceOffset]);
                term = X86Math.MultiplyQ16(term, car[GripOffset]);
                computed = X86Math.Negate(term);

                if (X86Math.Abs(car[GripOffset]) < GripLocalisationThreshold)
                {
                    computed = X86Math.TruncatePowerOfTwo(computed, 1);
                }
            }

            if (unchecked(context.Brake + context.Accelerator) < 0x1999)
            {
                selector = unchecked(selector + selector);
            }

            if (X86Math.Abs(car[ProjectedNormalOffset]) < GripLocalisationThreshold)
            {
                computed = X86Math.MultiplyQ16(X86Math.Abs(car[ProjectedNormalOffset]), AccumulatorStep);
            }

            int gravityProbe = X86Math.MultiplyQ16(0x1999, context.GravityFirst);
            int absorption = X86Math.MultiplyQ16(X86Math.Abs(computed), gravityProbe);
            int probe = unchecked(computed + absorption - car[NormalisedForceOffset]);
            int previous = car[NormalisedForceOffset];

            if (probe > selector)
            {
                selector = unchecked(previous + selector);
            }
            else if (probe >= unchecked(-selector))
            {
                selector = unchecked(previous + probe);
            }
            else
            {
                selector = unchecked(previous - selector);
            }

            car[NormalisedForceOffset] = selector;

            if (X86Math.Abs(selector) < NormalisedForceLimit && !brakeFlag)
            {
                ClearState(car);

                return;
            }

            if (car[ContactEventOffset] != 0)
            {
                ClearState(car);

                return;
            }

            if (X86Math.Abs(car[ResponseStateOffset]) == 2 && context.Accelerator < 0x8000)
            {
                ClearStateOnly(car);

                return;
            }

            if (X86Math.Abs(car[ResponseStateOffset]) > 2 && car[EngineSpeedOffset] < EngineMinimum)
            {
                ClearStateOnly(car);

                return;
            }

            if (car[PersistentSecondOffset] < AccumulatorStart)
            {
                car[PersistentSecondOffset] = Math.Min(
                    unchecked(car[PersistentSecondOffset] + AccumulatorIncrement), AccumulatorLimit);
            }

            bool advancesFirst =
                X86Math.Abs(track) > 0x4000 && X86Math.Abs(car[ResponseStateOffset]) != 2 ||
                X86Math.Abs(track) > 0x8000;

            if (advancesFirst && car[PersistentFirstOffset] < AccumulatorStart)
            {
                car[PersistentFirstOffset] = Math.Min(
                    unchecked(car[PersistentFirstOffset] + AccumulatorIncrement), AccumulatorLimit);
            }
        }

        private static int ReadLow(int index)
            => index >= 0 && index < TyreResponseTables.CapacityStates.Length
                ? TyreResponseTables.CapacityStates[index]
                : 0;

        /// <summary>
        /// The clear path taken when the derived force stays under <c>0x3333</c>. The state is
        /// cleared unless it is <c>+5</c> or <c>+1</c>, and the derived force is zeroed.
        /// </summary>
        private static void ClearState(CarMemory car)
        {
            if (X86Math.Abs(car[ResponseStateOffset]) != 5 && car[ResponseStateOffset] != 1)
            {
                car[ResponseStateOffset] = 0;
            }

            car[NormalisedForceOffset] = 0;
        }

        /// <summary>
        /// The clear path taken by the accelerator and engine-floor early returns. The state is
        /// cleared unless it is <c>+5</c>; the derived force is left untouched.
        /// </summary>
        private static void ClearStateOnly(CarMemory car)
        {
            if (X86Math.Abs(car[ResponseStateOffset]) != 5)
            {
                car[ResponseStateOffset] = 0;
            }
        }
    }
}