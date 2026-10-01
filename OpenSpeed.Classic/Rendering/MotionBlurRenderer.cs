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
        private readonly SpriteBatch spriteBatch = new(graphicsDevice);

        private short[] blurIndices = [];
        private int blurMeshHeight;
        private int blurMeshWidth;
        private VertexPositionColorTexture[] blurVertices = [];
        private RenderTarget2D? currentFrame;
        private bool hasPreviousFrame;
        private bool isDisposed;
        private RenderTarget2D? previousFrame;

        private static int BlurMeshColumnCount => 41;

        private static int BlurMeshRowCount => 25;

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
            EnsureBlurMesh(width, height);
            blurEffect.Texture = previousFrame;
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
                UpdateBlurVertices(
                    sampleEdgeStrength,
                    textureScale);
                DrawBlurSample();
            }
        }

        private void DrawBlurSample()
        {
            foreach (EffectPass pass in blurEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    blurVertices,
                    0,
                    blurVertices.Length,
                    blurIndices,
                    0,
                    blurIndices.Length / 3);
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

        private void EnsureBlurMesh(int width, int height)
        {
            if (blurMeshWidth == width && blurMeshHeight == height)
            {
                return;
            }

            int vertexCount = BlurMeshColumnCount * BlurMeshRowCount;
            int indexCount =
                (BlurMeshColumnCount - 1) *
                (BlurMeshRowCount - 1) *
                6;
            blurVertices = new VertexPositionColorTexture[vertexCount];
            blurIndices = new short[indexCount];
            blurMeshWidth = width;
            blurMeshHeight = height;
            PopulateBlurIndices();
        }

        private void PopulateBlurIndices()
        {
            int indexOffset = 0;

            for (int rowIndex = 0;
                rowIndex < BlurMeshRowCount - 1;
                rowIndex += 1)
            {
                for (int columnIndex = 0;
                    columnIndex < BlurMeshColumnCount - 1;
                    columnIndex += 1)
                {
                    short topLeft = (short)(
                        rowIndex * BlurMeshColumnCount + columnIndex);
                    short topRight = (short)(topLeft + 1);
                    short bottomLeft = (short)(topLeft + BlurMeshColumnCount);
                    short bottomRight = (short)(bottomLeft + 1);
                    blurIndices[indexOffset] = topLeft;
                    blurIndices[indexOffset + 1] = topRight;
                    blurIndices[indexOffset + 2] = bottomRight;
                    blurIndices[indexOffset + 3] = topLeft;
                    blurIndices[indexOffset + 4] = bottomRight;
                    blurIndices[indexOffset + 5] = bottomLeft;
                    indexOffset += 6;
                }
            }
        }

        private void SwapFrameTargets()
        {
            RenderTarget2D? renderedFrame = currentFrame;
            currentFrame = previousFrame;
            previousFrame = renderedFrame;
        }

        private void UpdateBlurVertices(
            float edgeStrength,
            float textureScale)
        {
            for (int rowIndex = 0;
                rowIndex < BlurMeshRowCount;
                rowIndex += 1)
            {
                float normalizedPositionY =
                    (float)rowIndex / (BlurMeshRowCount - 1);

                for (int columnIndex = 0;
                    columnIndex < BlurMeshColumnCount;
                    columnIndex += 1)
                {
                    float normalizedPositionX =
                        (float)columnIndex / (BlurMeshColumnCount - 1);
                    float strength = MotionBlurMaskCalculator.CalculateStrength(
                        normalizedPositionX,
                        normalizedPositionY,
                        edgeStrength);
                    int vertexIndex =
                        rowIndex * BlurMeshColumnCount + columnIndex;
                    blurVertices[vertexIndex] = CreateBlurVertex(
                        normalizedPositionX * blurMeshWidth,
                        normalizedPositionY * blurMeshHeight,
                        CalculateTextureCoordinate(normalizedPositionX, textureScale),
                        CalculateTextureCoordinate(normalizedPositionY, textureScale),
                        strength);
                }
            }
        }

        private static float CalculateSampleStrength(float combinedStrength)
            => 1.0f - MathF.Pow(
                1.0f - combinedStrength,
                1.0f / RadialBlurSampleCount);

        private static float CalculateTextureCoordinate(
            float textureCoordinate,
            float textureScale)
            => 0.5f + (textureCoordinate - 0.5f) * textureScale;

        private static VertexPositionColorTexture CreateBlurVertex(
            float positionX,
            float positionY,
            float textureCoordinateX,
            float textureCoordinateY,
            float strength)
            => new(
                new Vector3(positionX, positionY, 0.0f),
                Color.White * strength,
                new Vector2(textureCoordinateX, textureCoordinateY));

        private void ThrowIfDisposed()
            => ObjectDisposedException.ThrowIf(isDisposed, this);
    }
}