using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Translates <c>sub_451780</c>: saved-position restoration, the severity-gated simulation
    /// response, recovery-timer bookkeeping, and the final projection tail. Presentation
    /// callbacks (<c>sub_475E00</c>, <c>sub_451FC0</c>, <c>sub_475DA0</c>, <c>sub_4523F0</c>,
    /// <c>sub_46A3B0</c>, <c>sub_4540A0</c>) and the SimTune race rows are supplied explicitly,
    /// because the managed single-car model does not own the original race globals or the
    /// presentation dispatch.
    /// </summary>
    public static class PendingCollisionEventProcessor
    {
        private static int EventMagnitudeOffset => 0x160;

        private static int EventPartnerOffset => 0x164;

        private static int EventTypeOffset => 0x168;

        private static int CollisionCountOffset => 0x14C;

        private static int SevereEventOffset => 0x27C;

        private static int RecoveryTimerOffset => 0x578;

        private static int RecoverySuppressionOffset => 0x17C;

        private static int SavedPositionFlagOffset => 0x58C;

        private static int CorrectionOffset => 0x15C;

        private static int ProjectionOffset => 0x100;

        private static int VelocityOffset => 0xA8;

        private static int AngularOffset => 0xE8;

        private static int RoleFlagsOffset => 0x1F4;

        private static int SurfaceOffset => 0x184;

        private static int BasisRow1Offset => 0xD0;

        private static int NormalOffset => 0x124;

        private static int SevereEventFamily => 0x40000;

        private static int EventFamilyMask => unchecked((int)0xEFFF0000);

        private static int SpecialCarRoleMask => 0x40;

        private static int PlayerRoleMask => 0x04;

        private static int SupersonicRoleMask => 0x10;

        /// <summary>
        /// SimTune-derived race values. The source indexes two 29-dword rows as
        /// <c>mode * 29 + field</c>; the caller resolves that index and supplies the four
        /// recovered values.
        /// </summary>
        public readonly record struct RaceProfile(
            int EventMagnitudeThreshold,
            int CategoryTimer,
            int AngularSuppressionThreshold,
            int EarlyRecoveryThreshold);

        /// <summary>
        /// Injected native dependencies. <see cref="RefreshMotion"/> is <c>sub_46A3B0</c>,
        /// <see cref="ApplyFallback"/> is <c>sub_4540A0</c>, <see cref="RestorePosition"/> is
        /// <c>sub_475DA0(car + 0x9C)</c>, <see cref="Reposition"/> is <c>sub_4523F0(car, 0)</c>,
        /// and <see cref="DispatchPresentation"/> covers <c>sub_475E00</c> plus
        /// <c>sub_451FC0</c>.
        /// </summary>
        public readonly record struct Callbacks(
            Action<CarMemory> RefreshMotion,
            Action<CarMemory> ApplyFallback,
            Action<CarMemory> RestorePosition,
            Action<CarMemory> Reposition,
            Action<CarMemory> DispatchPresentation);

        public static void Update(
            CarMemory car,
            RaceProfile profile,
            Callbacks callbacks,
            int raceMode)
        {
            ArgumentNullException.ThrowIfNull(car);
            RestoreSavedPosition(car, callbacks);
            ConsumeEvent(car, profile, callbacks, raceMode);
            UpdateRecoveryTimer(car, profile, callbacks);
            DecrementPresentationTimers(car);
            ApplyProjectionTail(car, profile);
        }

        private static void RestoreSavedPosition(CarMemory car, Callbacks callbacks)
        {
            if (car[SavedPositionFlagOffset] == 0)
            {
                return;
            }

            callbacks.RestorePosition(car);

            if (car.ReadWord(CollisionCountOffset) == 0)
            {
                car[SavedPositionFlagOffset] = 0;
            }
        }

        /// <summary>
        /// <c>loc_451798..loc_4518F5</c>. A special car (role bit <c>0x40</c>) without a partner
        /// bypasses the simulation response; every other event above the profile threshold takes
        /// it. The event headers are cleared on every terminating path.
        /// </summary>
        private static void ConsumeEvent(
            CarMemory car,
            RaceProfile profile,
            Callbacks callbacks,
            int raceMode)
        {
            if (car[EventMagnitudeOffset] == 0)
            {
                return;
            }

            int family = car[EventTypeOffset] & EventFamilyMask;
            bool bypassesResponse = (car.ReadByte(RoleFlagsOffset) & SpecialCarRoleMask) != 0 &&
                car[EventPartnerOffset] == 0;

            if (!bypassesResponse && car[EventMagnitudeOffset] > profile.EventMagnitudeThreshold)
            {
                ApplySevereResponse(car, profile, callbacks, raceMode);
            }
            else if (family == SevereEventFamily &&
                car[VelocityOffset + sizeof(int)] > 0x180000 &&
                car[CorrectionOffset] < 0x9999)
            {
                callbacks.DispatchPresentation(car);
            }
            else if (car[EventMagnitudeOffset] > 0xA0000)
            {
                callbacks.DispatchPresentation(car);
            }
            else if (family == SevereEventFamily &&
                car[VelocityOffset + sizeof(int)] > 0xF0000 &&
                car[EventMagnitudeOffset] > 0xF0000 &&
                (car[SurfaceOffset] & 0xFF) <= 6)
            {
                callbacks.DispatchPresentation(car);
            }

            car[EventPartnerOffset] = 0;
            car[EventTypeOffset] = 0;
            car[EventMagnitudeOffset] = 0;
        }

        private static void ApplySevereResponse(
            CarMemory car,
            RaceProfile profile,
            Callbacks callbacks,
            int raceMode)
        {
            callbacks.RefreshMotion(car);
            car[SevereEventOffset] = unchecked(car[SevereEventOffset] + 1);

            if (car[CorrectionOffset] < X86Math.One)
            {
                int multiplier = 1;

                if (raceMode != 0 && (car.ReadByte(RoleFlagsOffset) & SpecialCarRoleMask) == 0)
                {
                    multiplier = 2;
                }

                int pitchImpulse = unchecked(multiplier * car[AngularOffset + sizeof(int)]);
                car[AngularOffset + sizeof(int) * 2] = unchecked(
                    car[AngularOffset + sizeof(int) * 2] + pitchImpulse);
                car[AngularOffset] = unchecked(car[AngularOffset] + pitchImpulse);
                callbacks.ApplyFallback(car);
            }

            callbacks.RestorePosition(car);
            car[SavedPositionFlagOffset] = 1;
            car[0x324] = 0;
            car[0x328] = 0;

            if (car[RecoveryTimerOffset] <= 0)
            {
                if (car[RecoverySuppressionOffset] != 0)
                {
                    car[RecoveryTimerOffset] = 0x100;
                }
                else
                {
                    int timer = profile.CategoryTimer;

                    if (raceMode == 1 && (car.ReadByte(RoleFlagsOffset) & 0x08) != 0)
                    {
                        timer = unchecked(timer * 3);
                    }

                    car[RecoveryTimerOffset] = timer;
                }
            }
        }

        private static void UpdateRecoveryTimer(CarMemory car, RaceProfile profile, Callbacks callbacks)
        {
            if (car[RecoveryTimerOffset] <= 0)
            {
                return;
            }

            bool skipDecrement = car[ProjectionOffset] >= 0x1999 &&
                car[RecoverySuppressionOffset] == 0 &&
                (car.ReadByte(RoleFlagsOffset) & PlayerRoleMask) != 0;

            if (!skipDecrement)
            {
                car[RecoveryTimerOffset] = unchecked(car[RecoveryTimerOffset] - 1);
            }

            if (car[CorrectionOffset] < 0x4CCC && car[ProjectionOffset] > 0x8000)
            {
                int magnitude = unchecked(X86Math.Abs(car[AngularOffset]) +
                    X86Math.Abs(car[AngularOffset + sizeof(int) * 2]));

                if (magnitude < profile.AngularSuppressionThreshold && car[RecoverySuppressionOffset] == 0)
                {
                    car[AngularOffset] = unchecked(car[AngularOffset] -
                        X86Math.TruncatePowerOfTwo(car[AngularOffset], 2));
                    car[AngularOffset + sizeof(int) * 2] = unchecked(
                        car[AngularOffset + sizeof(int) * 2] -
                        X86Math.TruncatePowerOfTwo(car[AngularOffset + sizeof(int) * 2], 2));
                    car[RecoveryTimerOffset] = unchecked(car[RecoveryTimerOffset] - 8);
                }
            }
            else if (car[CorrectionOffset] < 0x50000 && car[RecoverySuppressionOffset] == 0)
            {
                for (int component = 0; component < FixedMatrix.Dimension; component += 1)
                {
                    int offset = AngularOffset + component * sizeof(int);
                    car[offset] = unchecked(car[offset] - X86Math.TruncatePowerOfTwo(car[offset], 5));
                }
            }

            int combined = unchecked(X86Math.Abs(car[VelocityOffset + sizeof(int)]) + car[0xBC]);

            if (combined < X86Math.One && car[RecoverySuppressionOffset] == 0)
            {
                car[RecoveryTimerOffset] = unchecked(car[RecoveryTimerOffset] - 8);
            }

            if (car[RecoveryTimerOffset] <= 0 ||
                car[RecoveryTimerOffset] < profile.EarlyRecoveryThreshold &&
                car.ReadByte(0x2D4) > 0xC8 &&
                car[RecoverySuppressionOffset] == 0)
            {
                if ((car.ReadByte(RoleFlagsOffset) & SupersonicRoleMask) == 0)
                {
                    callbacks.Reposition(car);
                }

                car[RecoveryTimerOffset] = 0;
            }
        }

        private static void DecrementPresentationTimers(CarMemory car)
        {
            if (car[0x580] > 0)
            {
                car[0x580] = unchecked(car[0x580] - 1);
            }

            if (car[0x588] > 0)
            {
                car[0x588] = unchecked(car[0x588] - 1);
            }
        }

        /// <summary>
        /// <c>loc_451A9D..loc_451F6D</c>. Once the recovery timer has expired, a basis-row/normal
        /// projection recharge plus a collision-count angular kick keeps a resting body from
        /// settling permanently.
        /// </summary>
        private static void ApplyProjectionTail(CarMemory car, RaceProfile profile)
        {
            if (car[RecoveryTimerOffset] > 0)
            {
                return;
            }

            int projection = FixedVectors.Dot(
                FixedVectors.Read(car, BasisRow1Offset), FixedVectors.Read(car, NormalOffset));

            if (projection >= 0xC000)
            {
                int surface = car[SurfaceOffset];

                if (surface != 0 && surface != 0x0E && car[RecoverySuppressionOffset] == 0)
                {
                    return;
                }
            }

            if (car[RecoverySuppressionOffset] == 0)
            {
                car[RecoveryTimerOffset] = profile.CategoryTimer;
            }
            else
            {
                car[RecoveryTimerOffset] = 0x100;
            }

            if (car.ReadWord(CollisionCountOffset) != 0)
            {
                car[AngularOffset + sizeof(int) * 2] = unchecked(
                    car[AngularOffset + sizeof(int) * 2] + 0x51E);
                car[AngularOffset] = unchecked(car[AngularOffset] + 0x28F);
            }
        }
    }
}