using System;

namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal sealed class NeedForSpeed2TrackExtraBlock
    {
        private readonly byte[] payload;

        internal ReadOnlyMemory<byte> Payload => payload;

        internal ushort RecordCount { get; }

        internal ushort TypeIdentifier { get; }

        internal NeedForSpeed2TrackExtraBlock(
            ushort typeIdentifier,
            ushort recordCount,
            byte[] payload)
        {
            TypeIdentifier = typeIdentifier;
            RecordCount = recordCount;
            this.payload = payload;
        }
    }
}