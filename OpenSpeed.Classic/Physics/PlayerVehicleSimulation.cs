using System;

using OpenSpeed.Classic.Cars;

namespace OpenSpeed.Classic.Physics
{
    public sealed class PlayerVehicleSimulation : IPlayerVehicleSimulation
    {
        private readonly PhysicsRoute route;
        private readonly CarSpecifications descriptor;
        private readonly CarRuntimeType runtimeType = new();
        private readonly PhysicsContext context = new()
        {
            InputEnabled = 1,
            IsStartingDriveRangeForced = false
        };
        private readonly VehicleScheduler scheduler = new();
        private readonly RawVehicleInput input = new();
        private readonly byte[] surfaceLaunchFlags;
        private float movement;

        public CarMemory State { get; } = new();

        public bool IsGrounded { get; private set; }

        public PlayerVehicleSimulation(CarSpecifications source, PhysicsRoute route)
            : this(source, route, CarIdentifier.McLarenF1)
        {
        }

        public PlayerVehicleSimulation(CarSpecifications source, PhysicsRoute route, CarIdentifier carIdentifier)
            : this(source, route, carIdentifier, ReadOnlySpan<byte>.Empty)
        {
        }

        public PlayerVehicleSimulation(
            CarSpecifications source,
            PhysicsRoute route,
            CarIdentifier carIdentifier,
            FixedVector extents,
            int radius,
            ReadOnlySpan<byte> surfaceLaunchFlags)
            : this(source, route, carIdentifier, surfaceLaunchFlags)
        {
            PlayerStateInitialiser.Initialise(State, extents, radius);
            RouteBasis.LoadRecord(State, route, 0);
            FixedVectors.Write(State, 0x9C, route.Position(0));
            RouteBasis.CopyPrimary(State, false);
            RouteBasis.CopySecondary(State, false);
            IsGrounded = ProvisionalRouteContact.Resolve(State, route);
        }

        public PlayerVehicleSimulation(
            CarSpecifications source,
            PhysicsRoute route,
            CarIdentifier carIdentifier,
            ReadOnlySpan<byte> surfaceLaunchFlags)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(route);
            if (!Enum.IsDefined(carIdentifier))
            {
                throw new ArgumentOutOfRangeException(nameof(carIdentifier), carIdentifier, "The player car type is unsupported.");
            }

            this.route = route;
            this.surfaceLaunchFlags = surfaceLaunchFlags.ToArray();
            descriptor = new CarSpecifications();

            for (int offset = 0; offset < CarSpecifications.RuntimeSize; offset += sizeof(int))
            {
                descriptor[offset] = source[offset];
            }

            runtimeType[0] = (int)carIdentifier;
            runtimeType[0x08] = 1;
            runtimeType[0x10] = runtimeType[0x14] = runtimeType[0x18] = 1;
            runtimeType[0x1C] = runtimeType[0x20] = runtimeType[0x24] = runtimeType[0x2C] = X86Math.One;
            CarSpecificationsFinaliser.Finalise(descriptor, runtimeType, 0, 0);
            InitialiseState();
            scheduler.Register(VehicleSchedule.Sc32, 0x15, SampleControls);
            scheduler.Register(VehicleSchedule.Sc32, 0x1E, UpdateForces);
            scheduler.Register(VehicleSchedule.Sc32, 0x32, ApplySurfaceLaunch);
            scheduler.Register(VehicleSchedule.Sc64, 0x1E, IntegrateMotion);
        }

        public void Advance(TimeSpan elapsed, float movement, float steering, bool isHandbrakeApplied)
        {
            if (!float.IsFinite(movement) || !float.IsFinite(steering))
            {
                throw new ArgumentOutOfRangeException(nameof(movement), "Driving input must be finite.");
            }

            this.movement = Math.Clamp(movement, -1.0f, 1.0f);
            int steeringValue = (int)(Math.Clamp(steering, -1.0f, 1.0f) * 127.0f);
            input.SteeringWord = unchecked(steeringValue << 24);
            input.Actions = 0;

            if (isHandbrakeApplied)
            {
                input.Actions = 8;
            }

            scheduler.Advance(elapsed);
        }

        private void InitialiseState()
        {
            PlayerStateInitialiser.Initialise(State);
            State.WriteByte(0x1F4, 4);
            RouteBasis.LoadRecord(State, route, 0);
            FixedVectors.Write(State, 0x9C, route.Position(0));
            RouteBasis.CopyPrimary(State, false);
            RouteBasis.CopySecondary(State, false);
            IsGrounded = ProvisionalRouteContact.Resolve(State, route);
        }

        private void SampleControls()
        {
            context.BaseTick = scheduler.BaseTick;
            int speed = FixedVectors.Dot(FixedVectors.Read(State, 0xDC), FixedVectors.Read(State, 0xA8));
            bool isReverse = State.ReadByte(0x2DA) == 0;
            input.Accelerator = 0;
            input.Brake = 0;
            bool requestsReverse = movement < 0;

            if (movement != 0)
            {
                if (requestsReverse != isReverse && X86Math.Abs(speed) > 0x8000)
                {
                    input.Brake = (byte)(MathF.Abs(movement) * 248.0f);
                }
                else
                {
                    byte gear = 2;

                    if (requestsReverse)
                    {
                        gear = 0;
                    }

                    if (isReverse != requestsReverse || State.ReadByte(0x2DA) == 1)
                    {
                        State.WriteByte(0x2D6, gear);
                        ControlPipeline.ChangeGear(State, descriptor, context, gear);
                    }

                    input.Accelerator = (byte)(MathF.Abs(movement) * 248.0f);
                }
            }

            ControlPipeline.Sample(State, descriptor, runtimeType, context, input);
        }

        private void UpdateForces()
        {
            context.BaseTick = scheduler.BaseTick;

            if (!IsGrounded)
            {
                ControlPipeline.Smooth(State, descriptor, runtimeType, context);
                State[0x100] = 0;
            }

            ForceDispatcher.Update(State, context, IgnoreOpponentCollision, SolveMain, SolveAlternate);
            PendingCollisionEventConsumer.Consume(State);
        }

        private void SolveMain(CarMemory car)
            => MainForceSolver.Update(car, descriptor, runtimeType, context, route);

        private static void IgnoreOpponentCollision(CarMemory car)
        {
        }

        private void SolveAlternate(CarMemory car)
            => AlternateForceSolver.Update(car, descriptor, runtimeType, context, route);

        private void ApplySurfaceLaunch()
        {
            if (surfaceLaunchFlags.Length != 0)
            {
                SurfaceLaunch.TryApply(State, surfaceLaunchFlags);
            }
        }

        private void IntegrateMotion()
        {
            if (!IsGrounded)
            {
                State[0xAC] = unchecked(State[0xAC] - X86Math.TruncatePowerOfTwo(0xA0000, 6));
            }

            BodyMotion.IntegrateScaledPlayerPosition(State);
            BodyMotion.IntegratePlayerBasis(State);
            IsGrounded = ProvisionalRouteContact.Resolve(State, route);
        }
    }
}