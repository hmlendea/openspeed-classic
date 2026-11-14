namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal sealed class NeedForSpeed2ObjectPolygon
    {
        internal byte FirstVertexIndex { get; }

        internal byte FourthVertexIndex { get; }

        internal ushort LightingLevels { get; }

        internal ushort MaterialIdentifier { get; }

        internal byte SecondVertexIndex { get; }

        internal byte ThirdVertexIndex { get; }

        internal NeedForSpeed2ObjectPolygon(
            ushort materialIdentifier,
            ushort lightingLevels,
            byte firstVertexIndex,
            byte secondVertexIndex,
            byte thirdVertexIndex,
            byte fourthVertexIndex)
        {
            MaterialIdentifier = materialIdentifier;
            LightingLevels = lightingLevels;
            FirstVertexIndex = firstVertexIndex;
            SecondVertexIndex = secondVertexIndex;
            ThirdVertexIndex = thirdVertexIndex;
            FourthVertexIndex = fourthVertexIndex;
        }
    }
}