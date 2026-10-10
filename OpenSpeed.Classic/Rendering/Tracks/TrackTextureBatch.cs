using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    internal sealed class TrackTextureBatch : IDisposable
    {
        private static float MinimumBoundsRadius => 0.001f;

        private static float BoundsRadiusScale => 1.001f;

        internal int BlockIdentifier { get; }

        internal BoundingSphere Bounds { get; }

        internal TrackGeometryDetailLevel DetailLevel { get; }

        internal int PrimitiveCount { get; }

        internal Vector3 ResolutionCentre { get; }

        internal int TextureIdentifier { get; }

        internal VertexBuffer VertexBuffer { get; }

        internal TrackTextureBatch(
            GraphicsDevice graphicsDevice,
            int textureIdentifier,
            VertexPositionColorTexture[] vertices)
            : this(
                graphicsDevice,
                -1,
                TrackGeometryDetailLevel.Unrestricted,
                Vector3.Zero,
                textureIdentifier,
                vertices)
        {
        }

        internal TrackTextureBatch(
            GraphicsDevice graphicsDevice,
            int blockIdentifier,
            TrackGeometryDetailLevel detailLevel,
            Vector3 resolutionCentre,
            int textureIdentifier,
            VertexPositionColorTexture[] vertices)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            ArgumentNullException.ThrowIfNull(vertices);

            if (vertices.Length == 0)
            {
                throw new ArgumentException(
                    "At least one vertex is required to create a texture batch.",
                    nameof(vertices));
            }

            BlockIdentifier = blockIdentifier;
            Bounds = CreateBounds(vertices.Select(vertex => vertex.Position));
            DetailLevel = detailLevel;
            PrimitiveCount = vertices.Length / 3;
            ResolutionCentre = resolutionCentre;
            TextureIdentifier = textureIdentifier;
            VertexBuffer = new VertexBuffer(
                graphicsDevice,
                typeof(VertexPositionColorTexture),
                vertices.Length,
                BufferUsage.WriteOnly);
            VertexBuffer.SetData(vertices);
        }

        public void Dispose() => VertexBuffer.Dispose();

        private static BoundingSphere CreateBounds(
            System.Collections.Generic.IEnumerable<Vector3> points)
        {
            BoundingBox boundingBox = BoundingBox.CreateFromPoints(points);
            BoundingSphere bounds = BoundingSphere.CreateFromBoundingBox(boundingBox);

            return new BoundingSphere(
                bounds.Center,
                MathF.Max(
                    bounds.Radius * BoundsRadiusScale,
                    MinimumBoundsRadius));
        }
    }
}