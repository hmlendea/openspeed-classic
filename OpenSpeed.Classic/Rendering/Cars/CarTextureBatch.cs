using System;

using Microsoft.Xna.Framework.Graphics;

namespace OpenSpeed.Classic.Rendering.Cars
{
    internal sealed class CarTextureBatch : IDisposable
    {
        internal int PrimitiveCount { get; }

        internal string TextureName { get; }

        internal VertexBuffer VertexBuffer { get; }

        internal CarTextureBatch(
            GraphicsDevice graphicsDevice,
            string textureName,
            VertexPositionColorTexture[] vertices)
        {
            TextureName = textureName;
            PrimitiveCount = vertices.Length / 3;
            VertexBuffer = new VertexBuffer(
                graphicsDevice,
                typeof(VertexPositionColorTexture),
                vertices.Length,
                BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertices);
        }

        public void Dispose() => VertexBuffer.Dispose();
    }
}