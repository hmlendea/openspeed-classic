using System;

namespace OpenSpeed.Classic.Physics
{
    public static class ProvisionalRouteContact
    {
        private static int HalfCarWidth => 0x10000;

        private static int GroundTolerance => 0x100;

        public static bool Resolve(CarMemory car, PhysicsRoute route)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(route);

            int nearest = FindSegment(car, route);
            int next = (nearest + 1) % route.Count;
            FixedVector position = FixedVectors.Read(car, 0x9C);
            FixedVector start = route.Position(nearest);
            FixedVector end = route.Position(next);
            int fraction = ProjectFraction(position, start, end);
            FixedVector reference = Interpolate(start, end, fraction);
            FixedVector normal = Interpolate(route.Direction(nearest, 0x0C), route.Direction(next, 0x0C), fraction);
            FixedVector right = Interpolate(route.Direction(nearest, 0x12), route.Direction(next, 0x12), fraction);
            FixedVectors.NormaliseBasisVector(normal);
            FixedVectors.NormaliseBasisVector(right);
            car[0x14] = nearest;
            FixedVectors.Write(car, 0x13C, reference);
            FixedVectors.Write(car, 0x124, normal);
            FixedVectors.Write(car, 0x118, right);
            FixedVectors.Write(car, 0x130, route.Direction(nearest, 0x0F));
            car[0x148] = route.Heading(nearest);
            car[0x184] = 0;
            car[0x15C] = 0;
            if (normal.Second <= 0x1999)
            {
                return false;
            }

            ResolveWalls(car, route, nearest, next, fraction, right, reference);
            int height = ContactGeometry.CalculatePlaneHeight(car);
            int separation = unchecked(car[0xA0] - height);
            int normalVelocity = FixedVectors.Dot(normal, FixedVectors.Read(car, 0xA8));
            bool isGrounded = separation <= GroundTolerance &&
                (separation < -GroundTolerance || normalVelocity <= GroundTolerance);

            if (isGrounded)
            {
                car[0xA0] = height;
                FixedVector velocity = FixedVectors.Read(car, 0xA8);
                velocity.Second = X86Math.DivideQ16(unchecked(
                    -X86Math.MultiplyQ16(normal.First, velocity.First) -
                    X86Math.MultiplyQ16(normal.Third, velocity.Third)), normal.Second);
                FixedVectors.Write(car, 0xA8, velocity);
                AlignToSurface(car, normal);
            }

            return isGrounded;
        }

        private static void AlignToSurface(CarMemory car, FixedVector normal)
        {
            FixedVector forward = FixedVectors.Read(car, 0xDC);
            FixedVector right = FixedVectors.Cross(normal, forward);
            FixedVectors.NormaliseBasisVector(right);
            forward = FixedVectors.Cross(right, normal);
            FixedVectors.NormaliseBasisVector(forward);
            FixedVectors.Write(car, 0xC4, right);
            FixedVectors.Write(car, 0xD0, normal);
            FixedVectors.Write(car, 0xDC, forward);
            FixedMatrices.Write(car, 0x188, FixedMatrices.Read(car, 0xC4));
            car[0x100] = X86Math.One;
        }

        private static void ResolveWalls(CarMemory car, PhysicsRoute route, int index, int next, int fraction, FixedVector right, FixedVector reference)
        {
            FixedVector relative = FixedVectors.Subtract(FixedVectors.Read(car, 0x9C), reference);
            int lateral = FixedVectors.Dot(relative, right);
            int leftLimit = Math.Max(0, InterpolateScalar(route.ReadWord(index, 0x1A) << 8, route.ReadWord(next, 0x1A) << 8, fraction) - HalfCarWidth);
            int rightLimit = Math.Max(0, InterpolateScalar(route.ReadWord(index, 0x1C) << 8, route.ReadWord(next, 0x1C) << 8, fraction) - HalfCarWidth);
            int bounded = Math.Clamp(lateral, -leftLimit, rightLimit);

            if (bounded == lateral)
            {
                return;
            }

            FixedVectors.Write(car, 0x9C, FixedVectors.Add(FixedVectors.Read(car, 0x9C),
                FixedVectors.Scale(right, unchecked(bounded - lateral))));
            FixedVector velocity = FixedVectors.Read(car, 0xA8);
            int lateralVelocity = FixedVectors.Dot(velocity, right);

            if (lateral < bounded && lateralVelocity < 0 || lateral > bounded && lateralVelocity > 0)
            {
                FixedVector normal = right;

                if (lateral > bounded)
                {
                    normal = FixedVectors.Scale(right, -X86Math.One);
                }

                WorldContactResponse.Apply(car, FixedVectors.Read(car, 0x9C), normal, velocity);
            }
        }

        private static int FindSegment(CarMemory car, PhysicsRoute route)
        {
            FixedVector position = FixedVectors.Read(car, 0x9C);
            int nearest = RouteIndexSearch.Select(route, position, car[0x14]);
            int previous = (nearest + route.Count - 1) % route.Count;
            FixedVector start = route.Position(nearest);
            FixedVector end = route.Position((nearest + 1) % route.Count);
            FixedVector previousStart = route.Position(previous);
            FixedVector nextPoint = Interpolate(start, end, ProjectFraction(position, start, end));
            FixedVector previousPoint = Interpolate(previousStart, start, ProjectFraction(position, previousStart, start));

            if (PlanarDistance(position, previousPoint) < PlanarDistance(position, nextPoint))
            {
                return previous;
            }

            return nearest;
        }

        private static Int128 PlanarDistance(FixedVector position, FixedVector point)
        {
            long first = (long)position.First - point.First;
            long third = (long)position.Third - point.Third;

            return (Int128)first * first + (Int128)third * third;
        }

        private static int ProjectFraction(FixedVector position, FixedVector start, FixedVector end)
        {
            long first = (long)end.First - start.First;
            long third = (long)end.Third - start.Third;
            Int128 squared = (Int128)first * first + (Int128)third * third;

            if (squared == 0)
            {
                return 0;
            }

            Int128 projection = ((Int128)((long)position.First - start.First) * first +
                (Int128)((long)position.Third - start.Third) * third) * X86Math.One / squared;

            return (int)Int128.Clamp(projection, 0, X86Math.One);
        }

        private static FixedVector Interpolate(FixedVector start, FixedVector end, int fraction) => new()
        {
            First = InterpolateScalar(start.First, end.First, fraction),
            Second = InterpolateScalar(start.Second, end.Second, fraction),
            Third = InterpolateScalar(start.Third, end.Third, fraction)
        };

        private static int InterpolateScalar(int start, int end, int fraction)
            => unchecked(start + X86Math.MultiplyQ16(unchecked(end - start), fraction));
    }
}