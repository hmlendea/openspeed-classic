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