using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal static class NeedForSpeed2TrackRoadMarkingDecoder
    {
        private static ushort RoadLaneType => 9;

        private static int RoadLaneRecordSize => 4;

        private static byte SequenceTerminator => byte.MaxValue;

        internal static IEnumerable<TrackRoadMarking> Decode(
            IEnumerable<NeedForSpeed2TrackExtraBlock> extraBlocks,
            IEnumerable<TrackPoint> vertices)
        {
            ArgumentNullException.ThrowIfNull(extraBlocks);
            ArgumentNullException.ThrowIfNull(vertices);

            NeedForSpeed2TrackExtraBlock? laneBlock = extraBlocks.FirstOrDefault(
                extraBlock => extraBlock.TypeIdentifier == RoadLaneType);

            if (laneBlock is null)
            {
                return [];
            }

            TrackPoint[] vertexArray = vertices.ToArray();
            ReadOnlySpan<byte> payload = laneBlock.Payload.Span;
            long requiredSize = (long)laneBlock.RecordCount * RoadLaneRecordSize;

            if (requiredSize > payload.Length)
            {
                throw new InvalidDataException("The TRAC road-lane table is truncated.");
            }

            List<TrackRoadMarking> roadMarkings = [];
            List<TrackPoint> markingPoints = [];

            for (int recordIndex = 0; recordIndex < laneBlock.RecordCount; recordIndex += 1)
            {
                int recordOffset = recordIndex * RoadLaneRecordSize;
                int vertexIndex = payload[recordOffset];

                if (vertexIndex >= vertexArray.Length)
                {
                    throw new InvalidDataException(
                        $"TRAC road-lane record {recordIndex} references absent vertex " +
                        $"{vertexIndex}.");
                }

                markingPoints.Add(vertexArray[vertexIndex]);

                if (payload[recordOffset + 2] != SequenceTerminator)
                {
                    continue;
                }

                roadMarkings.Add(new TrackRoadMarking
                {
                    Identifier = roadMarkings.Count,
                    Points = [.. markingPoints]
                });
                markingPoints.Clear();
            }

            if (markingPoints.Count > 0)
            {
                throw new InvalidDataException(
                    "The TRAC road-lane table contains an unterminated sequence.");
            }

            return roadMarkings;
        }
    }
}