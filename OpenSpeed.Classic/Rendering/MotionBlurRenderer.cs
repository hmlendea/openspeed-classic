using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSpeed.Classic.Rendering
{
    public sealed class MotionBlurRenderer(
        GraphicsDevice graphicsDevice,
        float minimumSpeedKilometresPerHour) : IMotionBlurRenderer
    {
        private readonly BasicEffect blurEffect = new(graphicsDevice)
        {
            TextureEnabled = true,
            VertexColorEnabled = true
        };
        private readonly RasterizerState rasterizerState = new()
        {
            CullMode = CullMode.None
        };
        private readonly ScreenMaskMesh screenMaskMesh = new();
        private readonly SpriteBatch spriteBatch = new(graphicsDevice);

        private RenderTarget2D? currentFrame;
        private bool hasPreviousFrame;
        private bool isDisposed;
        private RenderTarget2D? previousFrame;

        private static int RadialBlurSampleCount => 4;

        private static float RadialBlurTextureScaleStep => 0.0125f;

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            DisposeFrameTargets();
            blurEffect.Dispose();
            rasterizerState.Dispose();
            spriteBatch.Dispose();
            isDisposed = true;
        }

        public void Draw(float longitudinalVelocity, Action drawScene)
        {
            ArgumentNullException.ThrowIfNull(drawScene);
            ThrowIfDisposed();

            float blurStrength = MotionBlurStrengthCalculator.Calculate(
                longitudinalVelocity,
                minimumSpeedKilometresPerHour);

            if (blurStrength == 0.0f)
            {
                hasPreviousFrame = false;
                graphicsDevice.SetRenderTarget(null);
                drawScene();

                return;
            }

            EnsureFrameTargets();
            graphicsDevice.SetRenderTarget(currentFrame);

            try
            {
                drawScene();
            }
            finally
            {
                graphicsDevice.SetRenderTarget(null);
            }

            DrawFrames(blurStrength);
            SwapFrameTargets();
            hasPreviousFrame = true;
        }

        private RenderTarget2D CreateFrameTarget(int width, int height)
            => new(
                graphicsDevice,
                width,
                height,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24,
                0,
                RenderTargetUsage.PreserveContents);

        private void DisposeFrameTargets()
        {
            currentFrame?.Dispose();
            currentFrame = null;
            previousFrame?.Dispose();
            previousFrame = null;
            hasPreviousFrame = false;
        }

        private void DrawFrames(float blurStrength)
        {
            int width = graphicsDevice.Viewport.Width;
            int height = graphicsDevice.Viewport.Height;
            Rectangle destination = new(0, 0, width, height);

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Opaque,
                SamplerState.LinearClamp);
            spriteBatch.Draw(currentFrame!, destination, Color.White);
            spriteBatch.End();

            if (!hasPreviousFrame)
            {
                return;
            }

            DrawPreviousFrameBlur(width, height, blurStrength);
        }

        private void DrawPreviousFrameBlur(
            int width,
            int height,
            float edgeStrength)
        {
            float sampleEdgeStrength = CalculateSampleStrength(edgeStrength);
            blurEffect.Texture = previousFrame;
            blurEffect.TextureEnabled = true;
            blurEffect.World = Matrix.Identity;
            blurEffect.View = Matrix.Identity;
            blurEffect.Projection = Matrix.CreateOrthographicOffCenter(
                0.0f,
                width,
                height,
                0.0f,
                0.0f,
                1.0f);
            graphicsDevice.BlendState = BlendState.AlphaBlend;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
            graphicsDevice.RasterizerState = rasterizerState;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

            for (int sampleIndex = 0;
                sampleIndex < RadialBlurSampleCount;
                sampleIndex += 1)
            {
                float textureScale = 1.0f -
                    RadialBlurTextureScaleStep * sampleIndex;
                screenMaskMesh.Update(
                    width,
                    height,
                    sampleEdgeStrength,
                    textureScale,
                    Color.White,
                    ScreenMaskType.MotionBlur);
                DrawScreenMask();
            }
        }

        private void DrawScreenMask()
        {
            foreach (EffectPass pass in blurEffect.CurrentTechnique.Passes)
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

        private void EnsureFrameTargets()
        {
            int width = graphicsDevice.Viewport.Width;
            int height = graphicsDevice.Viewport.Height;

            if (currentFrame is not null &&
                currentFrame.Width == width &&
                currentFrame.Height == height)
            {
                return;
            }

            DisposeFrameTargets();
            currentFrame = CreateFrameTarget(width, height);
            previousFrame = CreateFrameTarget(width, height);
        }

        private void SwapFrameTargets()
        {
            RenderTarget2D? renderedFrame = currentFrame;
            currentFrame = previousFrame;
            previousFrame = renderedFrame;
        }

        private static float CalculateSampleStrength(float combinedStrength)
            => 1.0f - MathF.Pow(
                1.0f - combinedStrength,
                1.0f / RadialBlurSampleCount);

        private void ThrowIfDisposed()
            => ObjectDisposedException.ThrowIf(isDisposed, this);
    }
}