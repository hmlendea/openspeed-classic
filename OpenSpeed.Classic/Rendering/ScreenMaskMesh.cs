using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSpeed.Classic.Rendering
{
    internal sealed class ScreenMaskMesh
    {
        private static int ColumnCount => 41;

        private static int RowCount => 25;

        private int height;

        private int width;

        internal short[] Indices { get; private set; } = [];

        internal VertexPositionColorTexture[] Vertices { get; private set; } = [];

        internal void Update(
            int width,
            int height,
            float maximumStrength,
            float textureScale,
            Color colour,
            ScreenMaskType maskType)
        {
            EnsureDimensions(width, height);

            for (int rowIndex = 0; rowIndex < RowCount; rowIndex += 1)
            {
                float normalizedPositionY = (float)rowIndex / (RowCount - 1);

                for (int columnIndex = 0;
                    columnIndex < ColumnCount;
                    columnIndex += 1)
                {
                    float normalizedPositionX =
                        (float)columnIndex / (ColumnCount - 1);
                    float strength = CalculateStrength(
                        normalizedPositionX,
                        normalizedPositionY,
                        maximumStrength,
                        maskType);
                    int vertexIndex = rowIndex * ColumnCount + columnIndex;
                    Vertices[vertexIndex] = new VertexPositionColorTexture(
                        new Vector3(
                            normalizedPositionX * width,
                            normalizedPositionY * height,
                            0.0f),
                        colour * strength,
                        new Vector2(
                            CalculateTextureCoordinate(
                                normalizedPositionX,
                                textureScale),
                            CalculateTextureCoordinate(
                                normalizedPositionY,
                                textureScale)));
                }
            }
        }

        private static float CalculateStrength(
            float normalizedPositionX,
            float normalizedPositionY,
            float maximumStrength,
            ScreenMaskType maskType)
            => maskType switch
            {
                ScreenMaskType.MotionBlur => MotionBlurMaskCalculator.CalculateStrength(
                    normalizedPositionX,
                    normalizedPositionY,
                    maximumStrength),
                ScreenMaskType.Vignette => VignetteMaskCalculator.CalculateDarkness(
                    normalizedPositionX,
                    normalizedPositionY),
                _ => throw new System.ArgumentOutOfRangeException(
                    nameof(maskType),
                    maskType,
                    "The screen mask type is not supported.")
            };

        private static float CalculateTextureCoordinate(
            float textureCoordinate,
            float textureScale)
            => 0.5f + (textureCoordinate - 0.5f) * textureScale;

        private void EnsureDimensions(int width, int height)
        {
            if (this.width == width && this.height == height)
            {
                return;
            }

            int vertexCount = ColumnCount * RowCount;
            int indexCount = (ColumnCount - 1) * (RowCount - 1) * 6;
            Vertices = new VertexPositionColorTexture[vertexCount];
            Indices = new short[indexCount];
            this.width = width;
            this.height = height;
            PopulateIndices();
        }

        private void PopulateIndices()
        {
            int indexOffset = 0;

            for (int rowIndex = 0; rowIndex < RowCount - 1; rowIndex += 1)
            {
                for (int columnIndex = 0;
                    columnIndex < ColumnCount - 1;
                    columnIndex += 1)
                {
                    short topLeft = (short)(rowIndex * ColumnCount + columnIndex);
                    short topRight = (short)(topLeft + 1);
                    short bottomLeft = (short)(topLeft + ColumnCount);
                    short bottomRight = (short)(bottomLeft + 1);
                    Indices[indexOffset] = topLeft;
                    Indices[indexOffset + 1] = topRight;
                    Indices[indexOffset + 2] = bottomRight;
                    Indices[indexOffset + 3] = topLeft;
                    Indices[indexOffset + 4] = bottomRight;
                    Indices[indexOffset + 5] = bottomLeft;
                    indexOffset += 6;
                }
            }
        }
    }
}