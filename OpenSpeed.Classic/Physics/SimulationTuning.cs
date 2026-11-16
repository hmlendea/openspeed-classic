using System;
using System.Buffers.Binary;
using System.IO;

namespace OpenSpeed.Classic.Physics
{
    public sealed class SimulationTuning
    {
        private readonly int[] values = new int[RowCount * ValuesPerRow];

        public static int RowCount => 2;

        public static int ValuesPerRow => 29;

        private static int ContiguousValueCount => 21;

        public int this[int mode, int field] => values[mode * ValuesPerRow + field];

        /// <summary>
        /// Resolves the four SimTune fields consumed by the pending-collision processor. The
        /// executable compares the event magnitude against <c>dword_511D48</c>, seeds the
        /// recovery timer from <c>dword_511D60</c>, compares the recovery timer against
        /// <c>dword_511D64</c>, and compares the summed angular magnitude against
        /// <c>dword_511D70</c>. Those globals are row fields 0, 6, 7, and 10, so the angular
        /// threshold is field 10 and the early-recovery threshold is field 7.
        /// </summary>
        public PendingCollisionEventProcessor.RaceProfile GetRaceProfile(int mode)
            => new(
                this[mode, 0],
                this[mode, 6],
                this[mode, 10],
                this[mode, 7]);

        public static SimulationTuning Decode(ReadOnlySpan<byte> source)
        {
            int requiredSize = RowCount * ValuesPerRow * sizeof(int);

            if (source.Length != requiredSize)
            {
                throw new InvalidDataException($"SimTune contains {source.Length} bytes; exactly {requiredSize} are required.");
            }

            SimulationTuning tuning = new();

            for (int row = 0; row < RowCount; row += 1)
            {
                for (int index = 0; index < ValuesPerRow; index += 1)
                {
                    int destination = index;

                    if (index >= ContiguousValueCount)
                    {
                        int pairIndex = index - ContiguousValueCount;
                        destination = ContiguousValueCount + pairIndex / 2 + pairIndex % 2 * 4;
                    }

                    tuning.values[row * ValuesPerRow + destination] = BinaryPrimitives.ReadInt32LittleEndian(
                        source.Slice((row * ValuesPerRow + index) * sizeof(int), sizeof(int)));
                }
            }

            return tuning;
        }
    }
}