namespace OpenSpeed.Classic.Tracks
{
    public sealed class TrackMaterial
    {
        public int Identifier { get; set; }

        public ushort Alignment { get; set; }

        public byte AnimationFrameCount { get; set; }

        public byte AnimationFrameInterval { get; set; }

        public TrackColour PrimaryColour { get; set; } = new();

        public TrackColour SecondaryColour { get; set; } = new();

        public int TextureIdentifier { get; set; }
    }
}