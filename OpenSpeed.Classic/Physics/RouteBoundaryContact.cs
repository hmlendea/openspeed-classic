using System;

namespace OpenSpeed.Classic.Physics
{
    /// <summary>
    /// Selects and applies lateral contact with the route boundaries.
    /// <para>
    /// The current velocity predicts the next position. That position is expressed relative to
    /// the active route record and projected onto the record's forward direction and the car's
    /// oriented half-extents, producing a supported depth. The two boundary words of the record
    /// are then tested against that depth with a fixed margin, and the crossed side selects the
    /// collision sign, the event displacement, the boundary surface class, and the response.
    /// </para>
    /// </summary>
    public static class RouteBoundaryContact
    {
        private static int RouteIndexOffset => 0x14;

        private static int PositionOffset => 0x9C;

        private static int VelocityOffset => 0xA8;

        private static int PrimaryBasisOffset => 0xC4;

        private static int ExtentOffset => 0x108;

        private static int SupportProjectionOffset => 0xC0;

        private static int EventMagnitudeOffset => 0x160;

        private static int EventPartnerOffset => 0x164;

        private static int EventTypeOffset => 0x168;

        private static int RecoveryTimerOffset => 0x578;

        private static int SavedPositionFlagOffset => 0x58C;

        private static int SuppressionOffset => 0x17C;

        private static int CorrectionOffset => 0x15C;

        private static int ProjectionOffset => 0x100;

        private static int CollisionCountOffset => 0x14C;

        private static int ReactionIntervalOffset => 0x53C;

        private static int FeatureFlagsOffset => 0x1F4;

        private static int AngularOffset => 0xE8;

        private static int AngularWordOffset => 0xF2;

        private static int AcceleratorOffset => 0x2D7;

        private static int ApproachOffset => 0xBC;

        private static int LeftBoundaryOffset => 0x1A;

        private static int RightBoundaryOffset => 0x1C;

        private static int DefinitionOffset => 0x1E;

        private static int ForwardOffset => 0x0F;

        private static int RightOffset => 0x12;

        private static int PredictionShift => 5;

        private static int BoundaryMargin => 0x4000;

        private static int BoundaryShift => 8;

        private static int FeatureFlagReaction => 0x04;

        private static int CorrectionNormalLimit => 0x9999;

        private static int ReactionProjectionThreshold => 0xE666;

        private static int PromotionApproach => 0x2CA3D7;

        private static int ApproachRaceType => 6;

        private static int RouteRaceType => 7;

        private static int RouteSurface => 6;

        private static int OverrideSurface => 7;

        private static int MediumRecoveryTrigger => 0x30000;

        private static int RecoveryTimer => 0x60;

        private static int RecoveryLateralImpulse => 0x8000;

        private static int RecoveryCentreImpulse => 0x3333;

        private static int SevereSpeedThreshold => 0xA0000;

        private static int SevereEventType => 0x40004;

        private static int SevereEventMagnitude => 0x1E0000;

        private static int SevereRecoveryTimer => 0x80;

        private static int SevereAngularImpulse => 0x18000;

        private static int SevereAcceleratorThreshold => 0xC8;

        private static int EventSurfaceBase => 0x40000;

        private static int[] RaceWindowLow => [0x0168, 0x0488, 0x04B0];

        private static int[] RaceWindowHigh => [0x0426, 0x0649, 0x06B9];

        /// <summary>
        /// Race-wide values the boundary response reads and records. They are not durable car state,
        /// which is why they are supplied by the caller rather than stored on the car.
        /// </summary>
        public sealed class Environment
        {
            /// <summary>
            /// Boundary surface class of the most recent crossed boundary. A race-window promotion
            /// replaces it with <see cref="OverrideSurface"/>.
            /// </summary>
            public int BoundarySurface { get; set; }

            /// <summary>Race type; the race-window promotion applies to the approach and route types.</summary>
            public int RaceType { get; set; }

            /// <summary>Nonzero enables the race-window surface promotion.</summary>
            public int RaceEventFlag { get; set; }

            /// <summary>Notified with the crossed boundary sign before the response runs.</summary>
            public Action<int>? NotifyBoundary { get; set; }

            /// <summary>
            /// Glancing response invoked with the car, the signed doubled penetration depth, and the
            /// record's contact normal. It owns the translation, orientation, and angular impulse
            /// applied along the boundary.
            /// </summary>
            public Action<CarMemory, int, FixedVector>? ApplyGlance { get; set; }

            /// <summary>Forced impulse applied after the severe boundary event.</summary>
            public Action<CarMemory>? ApplyForcedImpulse { get; set; }
        }

        /// <summary>
        /// Tests the route boundaries for the current car. Returns <c>0</c> when no boundary was
        /// crossed or the response was suppressed, <c>-1</c> for the ascending boundary, and
        /// <c>1</c> for the descending boundary.
        /// </summary>
        public static int Resolve(CarMemory car, PhysicsRoute route, Environment environment)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(environment);

            int routeIndex = car[RouteIndexOffset];
            FixedVector right = route.Direction(routeIndex, RightOffset);
            FixedVector relative = FixedVectors.Subtract(PredictPosition(car), route.Position(routeIndex));
            car[SupportProjectionOffset] = FixedVectors.Dot(right, relative);

            int support = CalculateSupportDepth(car, right);
            int boundarySurface = environment.BoundarySurface;
            int sign = 0;
            int penetration = 0;
            int ascendingLimit = unchecked(support - (route.ReadWord(routeIndex, LeftBoundaryOffset) << BoundaryShift) + BoundaryMargin);

            if (ascendingLimit > car[SupportProjectionOffset])
            {
                int definition = (route.ReadByte(routeIndex, DefinitionOffset) >> 4) & 0x0F;

                if (definition != 0)
                {
                    sign = -1;
                    penetration = unchecked(ascendingLimit - car[SupportProjectionOffset]);
                    boundarySurface = definition;
                }
            }

            int descendingLimit = unchecked((route.ReadWord(routeIndex, RightBoundaryOffset) << BoundaryShift) - support - BoundaryMargin);

            if (descendingLimit < car[SupportProjectionOffset])
            {
                int definition = route.ReadByte(routeIndex, DefinitionOffset) & 0x0F;

                if (definition != 0)
                {
                    sign = 1;
                    penetration = unchecked(car[SupportProjectionOffset] - descendingLimit);
                    boundarySurface = definition;
                }
            }

            if (sign == 0)
            {
                return 0;
            }

            environment.BoundarySurface = boundarySurface;
            environment.NotifyBoundary?.Invoke(sign);

            if (car[SuppressionOffset] != 0)
            {
                return 0;
            }

            FixedVector velocity = FixedVectors.Read(car, VelocityOffset);
            bool reacts = (car.ReadByte(FeatureFlagsOffset) & FeatureFlagReaction) != 0 || car[ReactionIntervalOffset] > 0;

            if (reacts)
            {
                bool isPromoted = environment.RaceEventFlag != 0 &&
                    car[ApproachOffset] > PromotionApproach &&
                    MatchesRaceWindow(environment.RaceType, routeIndex);

                if (isPromoted)
                {
                    boundarySurface = OverrideSurface;
                    environment.BoundarySurface = OverrideSurface;

                    return SevereOrResolve(car, route, sign, penetration, right, velocity, boundarySurface, environment, routeIndex);
                }

                if (boundarySurface == RouteSurface && car[CorrectionOffset] > MediumRecoveryTrigger)
                {
                    ApplyLightRecovery(car, routeIndex);

                    return 0;
                }

                if (boundarySurface == OverrideSurface)
                {
                    return SevereOrResolve(car, route, sign, penetration, right, velocity, boundarySurface, environment, routeIndex);
                }
            }

            return GlanceOrContact(car, route, sign, penetration, right, velocity, boundarySurface, environment, routeIndex);
        }

        /// <summary>
        /// The route-override outcome compares the absolute longitudinal speed with the severe
        /// threshold. Above it the severe event replaces the crossing; at or below it the crossing
        /// continues to the final selector.
        /// </summary>
        private static int SevereOrResolve(
            CarMemory car,
            PhysicsRoute route,
            int sign,
            int penetration,
            FixedVector right,
            FixedVector velocity,
            int boundarySurface,
            Environment environment,
            int routeIndex)
        {
            if (X86Math.Abs(FixedVectors.Dot(right, velocity)) > SevereSpeedThreshold)
            {
                ApplySevereEvent(car, environment, routeIndex);

                return 0;
            }

            return GlanceOrContact(car, route, sign, penetration, right, velocity, boundarySurface, environment, routeIndex);
        }

        /// <summary>
        /// The final selector. A boundary correction at or below the normal limit, a longitudinal
        /// projection at or above its threshold, and an empty collision count together select the
        /// glancing response; every other combination elevates the crossing to the centred
        /// world-contact response.
        /// </summary>
        private static int GlanceOrContact(
            CarMemory car,
            PhysicsRoute route,
            int sign,
            int penetration,
            FixedVector right,
            FixedVector velocity,
            int boundarySurface,
            Environment environment,
            int routeIndex)
        {
            if (car[CorrectionOffset] > CorrectionNormalLimit ||
                car[ProjectionOffset] < ReactionProjectionThreshold ||
                car.ReadWord(CollisionCountOffset) != 0)
            {
                ApplyWorldContact(car, sign, right, velocity, boundarySurface);

                return sign;
            }

            int doubledPenetration = unchecked(penetration + penetration);

            if (doubledPenetration <= 0)
            {
                doubledPenetration = unchecked(-doubledPenetration);
            }

            environment.ApplyGlance?.Invoke(car, unchecked(sign * doubledPenetration), route.Direction(routeIndex, ForwardOffset));

            return sign;
        }

        /// <summary>
        /// Applies the oriented world-contact response with the contact point at the car centre,
        /// which makes the lever arm zero. The contact normal points back into the corridor: the
        /// record right direction for the ascending boundary and its negation for the descending
        /// one. The recorded event keeps the crossed boundary surface class.
        /// </summary>
        private static void ApplyWorldContact(
            CarMemory car,
            int sign,
            FixedVector right,
            FixedVector velocity,
            int boundarySurface)
        {
            FixedVector normal = sign > 0 ? FixedVectors.Scale(right, -X86Math.One) : right;
            WorldContactResponse.Apply(car, FixedVectors.Read(car, PositionOffset), normal, velocity);

            if (car[EventMagnitudeOffset] != 0)
            {
                car[EventPartnerOffset] = 0;
                car[EventTypeOffset] = boundarySurface | EventSurfaceBase;
            }
        }

        /// <summary>
        /// The supported depth is the magnitude of each oriented half-extent projection onto the
        /// boundary direction: the third extent against the last primary-basis row and the first
        /// extent against the first row.
        /// </summary>
        private static int CalculateSupportDepth(CarMemory car, FixedVector right)
        {
            int third = X86Math.MultiplyQ16(
                car[ExtentOffset + sizeof(int) * 2],
                FixedVectors.Dot(FixedVectors.Read(car, PrimaryBasisOffset + sizeof(int) * (FixedMatrix.Dimension * 2)), right));
            int first = X86Math.MultiplyQ16(
                car[ExtentOffset],
                FixedVectors.Dot(FixedVectors.Read(car, PrimaryBasisOffset), right));

            return unchecked(X86Math.Abs(third) + X86Math.Abs(first));
        }

        /// <summary>
        /// Predicts the next position as the current position advanced by one thirty-second of the
        /// velocity with truncation toward zero.
        /// </summary>
        private static FixedVector PredictPosition(CarMemory car) => new()
        {
            First = unchecked(car[PositionOffset] + X86Math.TruncatePowerOfTwo(car[VelocityOffset], PredictionShift)),
            Second = unchecked(car[PositionOffset + sizeof(int)] + X86Math.TruncatePowerOfTwo(car[VelocityOffset + sizeof(int)], PredictionShift)),
            Third = unchecked(car[PositionOffset + sizeof(int) * 2] + X86Math.TruncatePowerOfTwo(car[VelocityOffset + sizeof(int) * 2], PredictionShift))
        };

        /// <summary>
        /// Race type six exposes two route windows and race type seven one; a route index inside a
        /// matching window promotes the boundary surface to the route override.
        /// </summary>
        private static bool MatchesRaceWindow(int raceType, int routeIndex)
        {
            if (raceType == ApproachRaceType)
            {
                return IsInsideWindow(routeIndex, 0) || IsInsideWindow(routeIndex, 1);
            }

            return raceType == RouteRaceType && IsInsideWindow(routeIndex, 2);
        }

        private static bool IsInsideWindow(int routeIndex, int window)
            => routeIndex > RaceWindowLow[window] && routeIndex < RaceWindowHigh[window];

        /// <summary>
        /// Starts the long boundary recovery, records the severe event, adds the severe angular and
        /// centre impulses, and applies the forced impulse when the accelerator is above the
        /// threshold.
        /// </summary>
        private static void ApplySevereEvent(CarMemory car, Environment environment, int routeIndex)
        {
            car[EventTypeOffset] = SevereEventType;
            car[EventPartnerOffset] = 0;
            car[RecoveryTimerOffset] = SevereRecoveryTimer;
            car[SavedPositionFlagOffset] = 1;
            car[EventMagnitudeOffset] = SevereEventMagnitude;
            car[SuppressionOffset] = routeIndex;
            car[AngularOffset] = unchecked(car[AngularOffset] + SevereAngularImpulse);
            car.WriteWord(AngularWordOffset, unchecked((ushort)(car.ReadWord(AngularWordOffset) + 1)));

            if (car.ReadByte(AcceleratorOffset) > SevereAcceleratorThreshold)
            {
                environment.ApplyForcedImpulse?.Invoke(car);
            }
        }

        /// <summary>
        /// Starts the short boundary recovery and adds a lateral and a centre impulse. This path
        /// records no event magnitude or type; it is the route-surface outcome taken when the
        /// boundary correction exceeds <see cref="MediumRecoveryTrigger"/>.
        /// </summary>
        private static void ApplyLightRecovery(CarMemory car, int routeIndex)
        {
            car[RecoveryTimerOffset] = RecoveryTimer;
            car[SavedPositionFlagOffset] = 1;
            car[SuppressionOffset] = routeIndex;
            car[AngularOffset] = unchecked(car[AngularOffset] + RecoveryLateralImpulse);
            car[AngularOffset + sizeof(int) * 2] = unchecked(car[AngularOffset + sizeof(int) * 2] + RecoveryCentreImpulse);
        }
    }
}
