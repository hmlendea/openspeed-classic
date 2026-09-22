using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackRouteDecoder
    {
        private static int OptionalPaddingSize => 2;

        private static int RoutePointSize => 36;

        private static ushort RoutePointType => 15;

        private static double FixedPointScale => 65536.0;

        private static int LeftBorderOffset => 26;

        private static int RightBorderOffset => 28;

        internal static IEnumerable<TrackRoutePoint> Decode(
            IEnumerable<NeedForSpeed2TrackExtraBlock> extraBlocks)
        {
            ArgumentNullException.ThrowIfNull(extraBlocks);

            NeedForSpeed2TrackExtraBlock? routeBlock = extraBlocks.FirstOrDefault(
                extraBlock => extraBlock.TypeIdentifier == RoutePointType);

            if (routeBlock is null)
            {
                return [];
            }

            ReadOnlySpan<byte> payload = routeBlock.Payload.Span;
            int requiredSize = routeBlock.RecordCount * RoutePointSize;
            int paddingSize = payload.Length - requiredSize;

            if (paddingSize != 0 && paddingSize != OptionalPaddingSize)
            {
                throw new InvalidDataException(
                    $"The route block declares {routeBlock.RecordCount} records, " +
                    $"but contains {payload.Length} payload bytes.");
            }

            TrackRoutePoint[] routePoints = new TrackRoutePoint[routeBlock.RecordCount];

            for (int routePointIndex = 0;
                routePointIndex < routePoints.Length;
                routePointIndex += 1)
            {
                int routePointOffset = routePointIndex * RoutePointSize;
                TrackVector right = ReadVector(payload, routePointOffset + 18);
                routePoints[routePointIndex] = new TrackRoutePoint
                {
                    Identifier = routePointIndex,
                    BlockIdentifier = BinaryPrimitives.ReadUInt16LittleEndian(
                        payload.Slice(routePointOffset + 22, sizeof(ushort))),
                    Position = ReadPoint(payload, routePointOffset),
                    Normal = ReadVector(payload, routePointOffset + 12),
                    Forward = ReadVector(payload, routePointOffset + 15),
                    Right = right,
                    LeftBorderDistance = ReadBorderDistance(
                        payload,
                        routePointOffset + LeftBorderOffset,
                        right),
                    RightBorderDistance = ReadBorderDistance(
                        payload,
                        routePointOffset + RightBorderOffset,
                        right)
                };
            }

            return routePoints;
        }

        private static TrackPoint ReadPoint(ReadOnlySpan<byte> payload, int offset)
        {
            int sourceX = BinaryPrimitives.ReadInt32LittleEndian(
                payload.Slice(offset, sizeof(int)));
            int sourceZ = BinaryPrimitives.ReadInt32LittleEndian(
                payload.Slice(offset + sizeof(int), sizeof(int)));
            int sourceY = BinaryPrimitives.ReadInt32LittleEndian(
                payload.Slice(offset + sizeof(int) * 2, sizeof(int)));

            return new TrackPoint
            {
                X = sourceX / FixedPointScale,
                Y = sourceZ / FixedPointScale,
                Z = -sourceY / FixedPointScale
            };
        }

        private static TrackVector ReadVector(ReadOnlySpan<byte> payload, int offset)
            => new()
            {
                X = unchecked((sbyte)payload[offset]),
                Y = unchecked((sbyte)payload[offset + 1]),
                Z = -unchecked((sbyte)payload[offset + 2])
            };

        private static double ReadBorderDistance(
            ReadOnlySpan<byte> payload,
            int offset,
            TrackVector right)
        {
            ushort border = BinaryPrimitives.ReadUInt16LittleEndian(
                payload.Slice(offset, sizeof(ushort)));
            double rightMagnitude = Math.Sqrt(
                right.X * right.X +
                right.Y * right.Y +
                right.Z * right.Z);

            return border * rightMagnitude * 2.0 / FixedPointScale;
        }
    }
}
