namespace OpenSpeed.Classic.Tracks
{
    public sealed class TrackRoutePoint
    {
        public int Identifier { get; set; }

        public int BlockIdentifier { get; set; }

        public TrackVector Forward { get; set; } = new();

        public TrackVector Normal { get; set; } = new();

        public TrackPoint Position { get; set; } = new();

        public TrackVector Right { get; set; } = new();
    }
}