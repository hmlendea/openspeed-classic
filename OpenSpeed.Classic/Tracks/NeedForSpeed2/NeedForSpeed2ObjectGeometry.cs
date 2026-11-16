namespace OpenSpeed.Classic.Tracks.NeedForSpeed2
{
    internal sealed class NeedForSpeed2ObjectGeometry
    {
        private readonly NeedForSpeed2ObjectPolygon[] polygons;
        private readonly NeedForSpeed2ObjectVertex[] vertices;

        internal int PolygonCount => polygons.Length;

        internal int VertexCount => vertices.Length;

        internal NeedForSpeed2ObjectGeometry(
            NeedForSpeed2ObjectVertex[] vertices,
            NeedForSpeed2ObjectPolygon[] polygons)
        {
            this.vertices = vertices;
            this.polygons = polygons;
        }

        internal NeedForSpeed2ObjectPolygon GetPolygon(int index) => polygons[index];

        internal NeedForSpeed2ObjectVertex GetVertex(int index) => vertices[index];
    }
}