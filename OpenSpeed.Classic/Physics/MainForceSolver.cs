using System;

namespace OpenSpeed.Classic.Physics
{
    public static class MainForceSolver
    {
        public static void Update(
            CarMemory car,
            CarSpecifications descriptor,
            CarRuntimeType runtimeType,
            PhysicsContext context,
            PhysicsRoute route,
            bool isAlternate = false)
        {
            car[0x2A4] = 0;
            car[0x2A8] = 0;
            car[0x2AC] = 0;
            car[0x200] = PhysicsTables.SurfaceMapNormal[car[0x184]];

            if ((context.FeatureFlags & 8) != 0)
            {
                car[0x200] = PhysicsTables.SurfaceMapAlternate[car[0x184]];
            }

            FixedMatrix basis = FixedMatrices.Read(car, 0x188);
            FixedVectors.Write(car, 0x2B0, FixedMatrices.Transform(basis, FixedVectors.Read(car, 0xA8)));
            FixedVectors.Write(car, 0x2BC, FixedMatrices.Transform(basis, FixedVectors.Read(car, 0xE8)));
            car[0x310] = 0;

            if (car[0x2B8] > 0x50000)
            {
                car[0x310] = X86Math.DivideQ16(car[0x2B0], car[0x2B8]);
            }

            car[0x304] = ContactResponse.Calculate(car[0x2B0], car[0x2B8]);

            ContactRecord front = CreateRecord(car, descriptor, true);
            ContactRecord rear = CreateRecord(car, descriptor, false);
            ProjectGravity(basis, context);
            ControlPipeline.Smooth(car, descriptor, runtimeType, context);
            front[0x0C] = CalculateSteering(car, descriptor, context);

            if (front[0x0C] != 0 && X86Math.Abs(car[0x2B8]) < X86Math.One &&
                car.ReadByte(0x2D7) < 0x20 && car.ReadByte(0x2DA) != 1)
            {
                DampMotion(car, 0xE666);
            }

            int force = Drivetrain.CalculateForce(car, descriptor, runtimeType, context);
            front[0] = X86Math.MultiplyQ16(force, descriptor[0xF8]);
            rear[0] = unchecked(force - front[0]);

            if (car.ReadWord(0x14C) != 0 && car[0x150] > descriptor[0x14C] || car[0x2E8] != 0)
            {
                car[0x324] = 0;
                car[0x328] = 0;

                return;
            }

            car[0x284] = 0;
            AxleForceDistribution distribution = Drivetrain.SplitForce(car, descriptor, runtimeType, context, force);
            front[0] = unchecked(front[0] + distribution.FrontBrake);
            rear[0] = unchecked(rear[0] + distribution.RearBrake);
            int baseLoad = X86Math.MultiplyQ16(unchecked(-context.GravitySecond), descriptor[0x138]);
            SurfaceResponse.Prepare(car, descriptor, runtimeType, context, route);
            int capacity = X86Math.MultiplyQ16(baseLoad, context.SurfaceCoefficient);

            if (capacity < 0)
            {
                return;
            }

            PrepareLoads(car, descriptor, context, capacity, front, rear);
            car[0x2FC] = 0;
            ContactSolver.UpdateRecord(car, descriptor, runtimeType, context, front);
            ContactSolver.UpdateRecord(car, descriptor, runtimeType, context, rear);
            CombineForces(car, descriptor, context, front, rear);
            int angularForce = X86Math.MultiplyQ16(unchecked(front[0x1C] - rear[0x1C]), descriptor[0x1D0]);
            angularForce = LimitAngularForce(car, descriptor, angularForce);
            int damping = CalculateAngularDamping(car, descriptor);
            SurfaceResponse.ApplyGravityResponse(car, context, isAlternate);
            LateralResponse.Apply(car, context, isAlternate);
            IntegrateForces(car, basis, angularForce, damping);
            ApplyNeutralDamping(car, context);
            car[0x2A4] = X86Math.TruncatePowerOfTwo(car[0x2A4], 2);
            car[0x2AC] = X86Math.TruncatePowerOfTwo(car[0x2AC], 1);
        }

        public static void DampMotion(CarMemory car, int coefficient)
        {
            FixedVectors.Write(car, 0xA8, FixedVectors.Scale(FixedVectors.Read(car, 0xA8), coefficient));
            FixedVectors.Write(car, 0xE8, FixedVectors.Scale(FixedVectors.Read(car, 0xE8), coefficient));
        }

        private static ContactRecord CreateRecord(CarMemory car, CarSpecifications descriptor, bool isFront)
        {
            int response = unchecked(-X86Math.MultiplyQ16(unchecked(car[0xEC] << 5), descriptor[0x1CC]));

            if (car[0x304] < 0x2CA45A)
            {
                response = X86Math.TruncatePowerOfTwo(response, 1);
            }
            else if (car[0x304] < 0x3C0000)
            {
                response = X86Math.TruncatePowerOfTwo(unchecked(response * 6), 3);
            }
            else if (car[0x304] > 0x500000)
            {
                response = X86Math.TruncatePowerOfTwo(unchecked(response * 6), 2);
            }

            ContactRecord record = new();
            record[0x10] = X86Math.TruncatePowerOfTwo(unchecked(-(car[0x2B0] << 5)), 1);
            record[0x14] = X86Math.TruncatePowerOfTwo(unchecked(-(car[0x2B4] << 5)), 1);
            record[0x18] = X86Math.TruncatePowerOfTwo(unchecked(-(car[0x2B8] << 5)), 1);

            if (isFront)
            {
                record[0x10] = unchecked(record[0x10] + response);
                record[0x28] = 1;
            }
            else
            {
                record[0x10] = unchecked(record[0x10] - response);
            }

            return record;
        }

        private static void ProjectGravity(FixedMatrix basis, PhysicsContext context)
        {
            FixedVector gravity = FixedMatrices.Transform(basis, new FixedVector { Second = -0xA0000 });
            context.GravityFirst = gravity.First;
            context.GravitySecond = gravity.Second;
            context.GravityThird = gravity.Third;
        }

        private static int CalculateSteering(CarMemory car, CarSpecifications descriptor, PhysicsContext context)
        {
            int gravity = X86Math.MultiplyQ16(X86Math.MultiplyQ16(context.GravityFirst, 0x30A3), 0x80000);
            int steering = Math.Clamp(unchecked(X86Math.TruncatePowerOfTwo(gravity, 16) + car[0x2E4]), -0x7F, 0x7F);
            int response = X86Math.TruncatePowerOfTwo(unchecked(descriptor[0x12C] * steering), 7);

            if (car[0x2B8] > 0x3C0000)
            {
                response = X86Math.DivideQ16(response, X86Math.MultiplyQ16(car[0x2B8], 0x444));
            }

            return response;
        }

        private static void PrepareLoads(CarMemory car, CarSpecifications descriptor, PhysicsContext context, int capacity, ContactRecord front, ContactRecord rear)
        {
            int firstCapacity = X86Math.MultiplyQ16(capacity, descriptor[0x128]);

            if (car[0x320] < 0)
            {
                car[0x320] = X86Math.TruncatePowerOfTwo(car[0x320], 1);
            }

            front[0x04] = X86Math.MultiplyQ16(unchecked(firstCapacity - car[0x320]), context.FrontCoefficient);
            rear[0x04] = X86Math.MultiplyQ16(unchecked(car[0x320] + capacity - firstCapacity), context.RearCoefficient);
        }

        private static void CombineForces(CarMemory car, CarSpecifications descriptor, PhysicsContext context, ContactRecord front, ContactRecord rear)
        {
            car[0x2AC] = unchecked(front[0x24] + rear[0x24]);
            car[0x2A4] = unchecked(front[0x1C] + rear[0x1C]);
            car[0x2AC] = X86Math.MultiplyQ16(car[0x2AC], descriptor[0x1D4]);
            car[0x2A8] = 0;
            car[0x320] = unchecked(X86Math.MultiplyQ16(car[0x2AC], descriptor[0x140]) -
                X86Math.MultiplyQ16(context.GravityThird, 0x1999));
        }

        private static int CalculateAngularDamping(CarMemory car, CarSpecifications descriptor)
        {
            if (car[0x2B8] <= 0 || X86Math.Abs(car[0x310]) <= 0xCCC ||
                X86Math.Abs(car[0x2F4]) >= 1 && car.ReadByte(0x2DC) == 0 || car[0x2B8] <= 0x50000)
            {
                return -1;
            }

            bool hasExtremeSteering = car.ReadByte(0x2DA) > 2 &&
                (car[0x2C0] > 0 && car[0x2E4] > 0x40 || car[0x2C0] < 0 && car[0x2E4] < -0x40);
            int scale = descriptor[0x150];

            if (hasExtremeSteering)
            {
                scale = X86Math.TruncatePowerOfTwo(scale, 1);

                if (car.ReadByte(0x2DC) != 0)
                {
                    scale = X86Math.TruncatePowerOfTwo(descriptor[0x150], 3);
                }
                else if (car.ReadByte(0x2D8) > 0)
                {
                    scale = unchecked(descriptor[0x150] * 2);
                }
            }
            else
            {
                if (car.ReadByte(0x2DC) != 0)
                {
                    return -1;
                }

                if (car.ReadByte(0x2D8) > 0)
                {
                    scale = unchecked(scale * 2);
                }
            }

            car[0x2F4] = 2;

            return Math.Max(unchecked(X86Math.One - X86Math.MultiplyQ16(X86Math.Abs(car[0x310]), scale)), 0x8000);
        }

        private static int LimitAngularForce(CarMemory car, CarSpecifications descriptor, int force)
        {
            if (car[0x2B8] <= 0 || X86Math.Abs(car[0x310]) <= 0xCCC ||
                X86Math.Abs(car[0x2F4]) < 1 || car.ReadByte(0x2DC) != 0)
            {
                return force;
            }

            if (car[0x2C0] > descriptor[0x14C] && force > 0 ||
                car[0x2C0] < unchecked(-descriptor[0x14C]) && force < 0)
            {
                int scale = Math.Max(unchecked(X86Math.One - X86Math.MultiplyQ16(X86Math.Abs(car[0x2B8]), 0x8000)), 0);
                force = X86Math.MultiplyQ16(force, scale);
            }

            return force;
        }

        private static void IntegrateForces(CarMemory car, FixedMatrix basis, int angularForce, int damping)
        {
            FixedMatrix inverse = FixedMatrices.Transpose(basis);
            FixedVector force = FixedMatrices.Transform(inverse, FixedVectors.Read(car, 0x2A4));
            FixedVector angular = FixedMatrices.Transform(inverse, new FixedVector { Second = angularForce });
            FixedVectors.Write(car, 0x28C, force);
            FixedVectors.Write(car, 0x298, angular);

            for (int component = 0; component < FixedMatrix.Dimension; component += 1)
            {
                int displacement = component * sizeof(int);
                car[0xA8 + displacement] = unchecked(car[0xA8 + displacement] + X86Math.TruncatePowerOfTwo(car[0x28C + displacement], 5));
                car[0xE8 + displacement] = unchecked(car[0xE8 + displacement] + X86Math.TruncatePowerOfTwo(car[0x298 + displacement], 5));
            }

            if (damping > 0)
            {
                FixedVector local = FixedMatrices.Transform(basis, FixedVectors.Read(car, 0xE8));
                local.Second = X86Math.MultiplyQ16(local.Second, damping);
                FixedVectors.Write(car, 0x2BC, local);
                FixedVectors.Write(car, 0xE8, FixedMatrices.Transform(inverse, local));
            }
        }

        private static void ApplyNeutralDamping(CarMemory car, PhysicsContext context)
        {
            if (car.ReadByte(0x2DA) != 1 || X86Math.Abs(context.GravityThird) >= 0x8000)
            {
                return;
            }

            int coefficient = 0xFD70;

            if (X86Math.Abs(car[0x2B8]) >= 0x140000 && X86Math.Abs(car[0x2E4]) <= 0x20)
            {
                coefficient = 0xFF7C;
            }

            DampMotion(car, coefficient);
        }
    }
}