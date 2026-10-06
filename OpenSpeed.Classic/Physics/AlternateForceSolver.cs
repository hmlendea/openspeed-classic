using System;

namespace OpenSpeed.Classic.Physics
{
    public static class AlternateForceSolver
    {
        private static int SurfaceOffset => 0x184;
        private static int MappedSurfaceOffset => 0x200;
        private static int PersistentForce1Offset => 0x324;
        private static int PersistentForce2Offset => 0x328;
        private static int HeadingReferenceOffset => 0x148;
        private static int ComparisonValueOffset => 0x204;
        private static int VelocityOffset => 0xA8;
        private static int ProjectedVelocityOffset => 0x2B0;
        private static int NormalisedForceOffset => 0x310;
        private static int GripOffset => 0x304;
        private static int ContactForceOffset => 0x30C;
        private static int AdditionalForceOffset => 0x30C;
        private static int ScratchOffset => 0x204;
        private static int CategoryOffset => 0x2DA;
        private static int ProjectedNormalOffset => 0x2B8;
        private static int SteeringOffset => 0x2E4;
        private static int AcceleratorSmoothedOffset => 0x2D7;
        private static int CarIndexOffset => 0x1E8;
        private static int LinearVelocityOffset => 0xA8;
        private static int AngularVelocityOffset => 0xE8;
        private static int TransformedForceOffset => 0x28C;
        private static int LocalForceOffset => 0x2A4;
        private static int LocalForceYOffset => 0x2A8;
        private static int LocalForceZOffset => 0x2AC;
        private static int BasisRow1Offset => 0xDC;
        private static int BasisRow2Offset => 0xE4;
        private static int AngularRateOffset => 0xE8;

        public static void Update(
            CarMemory car,
            CarSpecifications descriptor,
            CarRuntimeType runtimeType,
            PhysicsContext context,
            PhysicsRoute route)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentNullException.ThrowIfNull(runtimeType);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(route);

            int surface = car[SurfaceOffset];
            if ((context.FeatureFlags & 8) == 0)
            {
                surface = PhysicsTables.SurfaceMapNormal[surface];
            }
            else
            {
                surface = PhysicsTables.SurfaceMapAlternate[surface];
            }
            car[MappedSurfaceOffset] = surface & 0xFF;

            car[PersistentForce1Offset] = X86Math.TruncatePowerOfTwo(car[PersistentForce1Offset], 1);
            car[PersistentForce2Offset] = X86Math.TruncatePowerOfTwo(car[PersistentForce2Offset], 1);

            HeadingDifference.Calculate(car, route.Count, route.Heading);

            FixedMatrix basis = FixedMatrices.Read(car, 0x188);
            FixedVectors.Write(car, ProjectedVelocityOffset, FixedMatrices.Transform(basis, FixedVectors.Read(car, VelocityOffset)));

            car[GripOffset] = ContactResponse.Calculate(car[ProjectedVelocityOffset], car[ProjectedNormalOffset]);

            ControlPipeline.Smooth(car, descriptor, runtimeType, context);
            TyreStateMachine.Update(car, descriptor, runtimeType, context);

            int force = Drivetrain.CalculateForce(car, descriptor, runtimeType, context);
            int combinedForce = unchecked(car[GripOffset] + car[NormalisedForceOffset] + car[AdditionalForceOffset]);

            FixedMatrix inverse = FixedMatrices.Transpose(basis);
            FixedVector localForce = FixedMatrices.Transform(inverse, new FixedVector
            {
                First = 0,
                Second = combinedForce,
                Third = 0
            });

            LateralResponse.Apply(car, context, true);

            FixedVectors.Write(car, TransformedForceOffset, FixedMatrices.Transform(basis, FixedVectors.Read(car, LocalForceOffset)));

            for (int i = 0; i < 3; i++)
            {
                int displacement = i * sizeof(int);
                car[LinearVelocityOffset + displacement] = unchecked(car[LinearVelocityOffset + displacement] + X86Math.TruncatePowerOfTwo(car[TransformedForceOffset + displacement], 5));
                car[AngularVelocityOffset + displacement] = unchecked(car[AngularVelocityOffset + displacement] + X86Math.TruncatePowerOfTwo(car[TransformedForceOffset + 0xC + displacement], 5));
            }

            if (car.ReadByte(CategoryOffset) == 1 && car[ProjectedNormalOffset] > 0 && car[ProjectedNormalOffset] < 0x140000)
            {
                DampMotion(car, 0xFEB8);
            }

            car[ScratchOffset] = 0;
            car[LocalForceYOffset] = X86Math.TruncatePowerOfTwo(car[LocalForceYOffset], 3);
            car[LocalForceZOffset] = X86Math.TruncatePowerOfTwo(car[LocalForceZOffset], 1);
        }

        private static void DampMotion(CarMemory car, int coefficient)
        {
            FixedVectors.Write(car, LinearVelocityOffset, FixedVectors.Scale(FixedVectors.Read(car, LinearVelocityOffset), coefficient));
            FixedVectors.Write(car, AngularVelocityOffset, FixedVectors.Scale(FixedVectors.Read(car, AngularVelocityOffset), coefficient));
        }
    }
}