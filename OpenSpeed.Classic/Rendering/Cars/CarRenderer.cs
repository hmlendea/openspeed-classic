using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Rendering.Tracks;

namespace OpenSpeed.Classic.Rendering.Cars
{
    public sealed class CarRenderer(GraphicsDevice graphicsDevice) : ICarRenderer
    {
        private readonly BasicEffect colourEffect = new(graphicsDevice)
        {
            VertexColorEnabled = true
        };
        private readonly RasterizerState rasterizerState = new()
        {
            CullMode = CullMode.None
        };
        private readonly AlphaTestEffect textureEffect = new(graphicsDevice)
        {
            AlphaFunction = CompareFunction.Greater,
            ReferenceAlpha = 0x10,
            VertexColorEnabled = true
        };

        private int colourPrimitiveCount;
        private VertexBuffer? colourVertexBuffer;
        private bool isDisposed;
        private CarTextureBatch[] textureBatches = [];
        private Dictionary<string, CarTextureResource> textureResources =
            new(StringComparer.Ordinal);

        public bool HasGeometry
            => colourVertexBuffer is not null || textureBatches.Length > 0;

        private static Color FallbackColour => new(160, 160, 160);

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            DisposeCarResources();
            rasterizerState.Dispose();
            colourEffect.Dispose();
            textureEffect.Dispose();
            isDisposed = true;
        }

        public void Draw(
            TrackCamera camera,
            Matrix world,
            int viewportWidth,
            int viewportHeight)
        {
            ArgumentNullException.ThrowIfNull(camera);
            ThrowIfDisposed();

            if (!HasGeometry || viewportWidth <= 0 || viewportHeight <= 0)
            {
                return;
            }

            Matrix view = camera.CreateView();
            Matrix projection = camera.CreateProjection(viewportWidth, viewportHeight);
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.RasterizerState = rasterizerState;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            ConfigureEffect(colourEffect, world, view, projection);
            ConfigureEffect(textureEffect, world, view, projection);
            DrawColouredGeometry();
            DrawTexturedGeometry();
        }

        public void Load(LoadedCar car)
        {
            ArgumentNullException.ThrowIfNull(car);
            ThrowIfDisposed();
            DisposeCarResources();

            foreach (CarTexture carTexture in car.Textures)
            {
                CarTextureResource textureResource = new(graphicsDevice, carTexture);
                textureResources[textureResource.Name] = textureResource;
            }

            List<VertexPositionColor> colouredVertices = [];
            Dictionary<string, List<VertexPositionColorTexture>> texturedVertices =
                new(StringComparer.Ordinal);

            foreach (CarGeometryTriangle triangle in car.Geometry)
            {
                if (!textureResources.ContainsKey(triangle.TextureName))
                {
                    colouredVertices.AddRange(
                        CarGeometryVertexBuilder.BuildColoured(
                            triangle,
                            FallbackColour));

                    continue;
                }

                if (!texturedVertices.ContainsKey(triangle.TextureName))
                {
                    texturedVertices.Add(triangle.TextureName, []);
                }

                texturedVertices[triangle.TextureName].AddRange(
                    CarGeometryVertexBuilder.BuildTextured(triangle, Color.White));
            }

            CreateColourBuffer(colouredVertices);
            textureBatches = texturedVertices
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new CarTextureBatch(
                    graphicsDevice,
                    pair.Key,
                    pair.Value.ToArray()))
                .ToArray();
        }

        private static void ConfigureEffect(
            BasicEffect effect,
            Matrix world,
            Matrix view,
            Matrix projection)
        {
            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
        }

        private static void ConfigureEffect(
            AlphaTestEffect effect,
            Matrix world,
            Matrix view,
            Matrix projection)
        {
            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
        }

        private void CreateColourBuffer(IEnumerable<VertexPositionColor> vertices)
        {
            VertexPositionColor[] vertexArray = vertices.ToArray();

            if (vertexArray.Length == 0)
            {
                return;
            }

            colourPrimitiveCount = vertexArray.Length / 3;
            colourVertexBuffer = new VertexBuffer(
                graphicsDevice,
                typeof(VertexPositionColor),
                vertexArray.Length,
                BufferUsage.WriteOnly);
            colourVertexBuffer.SetData(vertexArray);
        }

        private void DisposeCarResources()
        {
            colourVertexBuffer?.Dispose();
            colourVertexBuffer = null;
            colourPrimitiveCount = 0;

            foreach (CarTextureBatch textureBatch in textureBatches)
            {
                textureBatch.Dispose();
            }

            foreach (CarTextureResource textureResource in textureResources.Values)
            {
                textureResource.Dispose();
            }

            textureBatches = [];
            textureResources = new Dictionary<string, CarTextureResource>(
                StringComparer.Ordinal);
        }

        private void DrawColouredGeometry()
        {
            if (colourVertexBuffer is null)
            {
                return;
            }

            foreach (EffectPass effectPass in colourEffect.CurrentTechnique.Passes)
            {
                effectPass.Apply();
                graphicsDevice.SetVertexBuffer(colourVertexBuffer);
                graphicsDevice.DrawPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    colourPrimitiveCount);
            }
        }

        private void DrawTexturedGeometry()
        {
            foreach (CarTextureBatch textureBatch in textureBatches)
            {
                textureEffect.Texture = textureResources[textureBatch.TextureName].Texture;

                foreach (EffectPass effectPass in textureEffect.CurrentTechnique.Passes)
                {
                    effectPass.Apply();
                    graphicsDevice.SetVertexBuffer(textureBatch.VertexBuffer);
                    graphicsDevice.DrawPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        textureBatch.PrimitiveCount);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(CarRenderer));
            }
        }
    }
}