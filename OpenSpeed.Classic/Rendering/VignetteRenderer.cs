using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSpeed.Classic.Rendering
{
    public sealed class VignetteRenderer(GraphicsDevice graphicsDevice) : IVignetteRenderer
    {
        private readonly BasicEffect effect = new(graphicsDevice)
        {
            TextureEnabled = false,
            VertexColorEnabled = true
        };
        private readonly RasterizerState rasterizerState = new()
        {
            CullMode = CullMode.None
        };
        private readonly ScreenMaskMesh screenMaskMesh = new();

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            effect.Dispose();
            rasterizerState.Dispose();
            isDisposed = true;
        }

        public void Draw()
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            int width = graphicsDevice.Viewport.Width;
            int height = graphicsDevice.Viewport.Height;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            screenMaskMesh.Update(
                width,
                height,
                VignetteMaskCalculator.MaximumDarkness,
                1.0f,
                Color.Black,
                ScreenMaskType.Vignette);
            effect.World = Matrix.Identity;
            effect.View = Matrix.Identity;
            effect.Projection = Matrix.CreateOrthographicOffCenter(
                0.0f,
                width,
                height,
                0.0f,
                0.0f,
                1.0f);
            graphicsDevice.BlendState = BlendState.AlphaBlend;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
            graphicsDevice.RasterizerState = rasterizerState;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    screenMaskMesh.Vertices,
                    0,
                    screenMaskMesh.Vertices.Length,
                    screenMaskMesh.Indices,
                    0,
                    screenMaskMesh.Indices.Length / 3);
            }
        }
    }
}