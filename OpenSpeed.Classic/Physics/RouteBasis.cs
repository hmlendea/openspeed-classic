using System;
using System.Buffers.Binary;

namespace OpenSpeed.Classic.Physics
{
    public static class RouteBasis
    {
        private static int PrimaryBasisOffset => 0xC4;

        private static int SecondaryBasisOffset => 0x188;

        private static int RouteBasisOffset => 0x118;

        private static int ReferencePointOffset => 0x13C;

        private static int HeadingOffset => 0x148;

        private static int RightRecordOffset => 0x12;

        private static int NormalRecordOffset => 0x0C;

        private static int ForwardRecordOffset => 0x0F;

        private static int ActiveBodyOffset => 0x8D;

        private static int ContactModeOffset => 0x8C;

        private static int RouteIndexOffset => 0x14;

        private static int ExtensionOffset => 0x18;

        private static int ExtensionReferenceOffset => 0x28;

        private static int ExtensionDirectionSize => 12;

        /// <summary>
        /// Refreshes sub_462FA0's route basis with an explicit native assertion/query dependency
        /// and the twelve bytes addressed by the extension direction pointer when present.
        /// </summary>
        public static void Refresh(
            CarMemory car,
            PhysicsRoute route,
            Func<CarMemory, bool> queryExtensionReference,
            ReadOnlySpan<byte> extensionDirections)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(route);
            ArgumentNullException.ThrowIfNull(queryExtensionReference);

            if (car.ReadByte(ActiveBodyOffset) == 0)
            {
                return;
            }

            if (car.ReadByte(ContactModeOffset) != 0)
            {
                LoadRecord(car, route, car[RouteIndexOffset]);

                return;
            }

            bool usesExtensionReference = queryExtensionReference(car);
            RefreshOrdinary(car, route, usesExtensionReference, extensionDirections);
        }

        private static void RefreshOrdinary(
            CarMemory car,
            PhysicsRoute route,
            bool usesExtensionReference,
            ReadOnlySpan<byte> extensionDirections)
        {
            int index = car[RouteIndexOffset];
            FixedVector reference = route.Position(index);

            if (usesExtensionReference && car[ExtensionOffset] != 0)
            {
                reference = FixedVectors.Read(car, ExtensionReferenceOffset);
            }

            FixedVectors.Write(car, ReferencePointOffset, reference);
            FixedVector normal = route.Direction(index, NormalRecordOffset);
            FixedVector forward = route.Direction(index, ForwardRecordOffset);

            if (car[ExtensionOffset] != 0)
            {
                if (extensionDirections.Length < ExtensionDirectionSize)
                {
                    throw new ArgumentException(
                        $"The extension direction buffer contains {extensionDirections.Length} bytes; at least {ExtensionDirectionSize} are required.",
                        nameof(extensionDirections));
                }

                normal = ReadExtensionDirection(extensionDirections);
                forward = ReadExtensionDirection(extensionDirections.Slice(ExtensionDirectionSize / 2));
            }

            FixedVectors.Write(car, RouteBasisOffset + FixedMatrix.Dimension * sizeof(int), normal);
            FixedVectors.Write(car, RouteBasisOffset + FixedMatrix.Dimension * sizeof(int) * 2, forward);
            FixedVectors.Write(car, RouteBasisOffset, FixedVectors.Cross(normal, forward));
            car[HeadingOffset] = route.Heading(index);
        }

        private static FixedVector ReadExtensionDirection(ReadOnlySpan<byte> directions) => new()
        {
            First = BinaryPrimitives.ReadInt16LittleEndian(directions) << 1,
            Second = BinaryPrimitives.ReadInt16LittleEndian(directions.Slice(sizeof(short))) << 1,
            Third = BinaryPrimitives.ReadInt16LittleEndian(directions.Slice(sizeof(short) * 2)) << 1
        };

        /// <summary>
        /// Refreshes the direct-record branch of sub_462FA0 for an active contact-mode body.
        /// Returns false without writes when the body is inactive or ordinary queries are required.
        /// </summary>
        public static bool TryRefreshContactMode(CarMemory car, PhysicsRoute route)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(route);

            if (car.ReadByte(ActiveBodyOffset) == 0 ||
                car.ReadByte(ContactModeOffset) == 0)
            {
                return false;
            }

            LoadRecord(car, route, car[RouteIndexOffset]);

            return true;
        }

        public static void LoadRecord(CarMemory car, PhysicsRoute route, int index)
        {
            ArgumentNullException.ThrowIfNull(car);
            ArgumentNullException.ThrowIfNull(route);

            FixedVectors.Write(car, ReferencePointOffset, route.Position(index));
            FixedVectors.Write(car, RouteBasisOffset, route.Direction(index, RightRecordOffset));
            FixedVectors.Write(car, RouteBasisOffset + FixedMatrix.Dimension * sizeof(int), route.Direction(index, NormalRecordOffset));
            FixedVectors.Write(car, RouteBasisOffset + FixedMatrix.Dimension * sizeof(int) * 2, route.Direction(index, ForwardRecordOffset));
            car[HeadingOffset] = route.Heading(index);
        }

        public static void CopyPrimary(CarMemory car, bool isReversed)
            => CopyBasis(car, PrimaryBasisOffset, isReversed);

        public static void CopySecondary(CarMemory car, bool isReversed)
            => CopyBasis(car, SecondaryBasisOffset, isReversed);

        private static void CopyBasis(CarMemory car, int destination, bool isReversed)
        {
            ArgumentNullException.ThrowIfNull(car);

            for (int row = 0; row < FixedMatrix.Dimension; row += 1)
            {
                for (int column = 0; column < FixedMatrix.Dimension; column += 1)
                {
                    int displacement = (row * FixedMatrix.Dimension + column) * sizeof(int);
                    int value = car[RouteBasisOffset + displacement];

                    if (isReversed && row != 1)
                    {
                        value = unchecked(-value);
                    }

                    car[destination + displacement] = value;
                }
            }
        }
    }
}