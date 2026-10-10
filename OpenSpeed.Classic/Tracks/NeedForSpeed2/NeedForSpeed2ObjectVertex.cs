namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal sealed class NeedForSpeed2ObjectVertex
    {
        internal short SourceX { get; }

        internal short SourceY { get; }

        internal short SourceZ { get; }

        internal NeedForSpeed2ObjectVertex(short sourceX, short sourceZ, short sourceY)
        {
            SourceX = sourceX;
            SourceZ = sourceZ;
            SourceY = sourceY;
        }
    }
}