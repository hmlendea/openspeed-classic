using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NuciXNA.Gui;
using NuciXNA.Gui.Controls;
using NuciXNA.Primitives;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering
{
    public sealed class MinimapRenderer(GraphicsDevice graphicsDevice, Color playerColour) : IMinimapRenderer
    {
        private readonly GuiImage background = new()
        {
            ContentFile = BackgroundContentName,
            SamplerState = SamplerState.LinearClamp
        };
        private readonly GuiManager guiManager = new();
        private readonly SpriteBatch spriteBatch = new(graphicsDevice);
        private readonly BasicEffect effect = new(graphicsDevice) { VertexColorEnabled = true };
        private readonly RasterizerState rasterizerState = new() { CullMode = CullMode.None };
        private readonly BlendState maskBlendState = new() { ColorWriteChannels = ColorWriteChannels.None };
        private readonly DepthStencilState maskWriteState = new()
        {
            DepthBufferEnable = false,
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilPass = StencilOperation.Replace,
            ReferenceStencil = 1
        };
        private readonly DepthStencilState maskReadState = new()
        {
            DepthBufferEnable = false,
            StencilEnable = true,
            StencilFunction = CompareFunction.Equal,
            StencilPass = StencilOperation.Keep,
            ReferenceStencil = 1
        };
        private readonly VertexPositionColor[] maskVertices = CreateMaskVertices();
        private readonly VertexPositionColor[] markerVertices =
        [
            new(new Vector3(0.0f, -2.0f, 0.0f), playerColour),
            new(new Vector3(-1.0f, 1.0f, 0.0f), playerColour),
            new(new Vector3(1.0f, 1.0f, 0.0f), playerColour)
        ];
        private VertexPositionColor[] roadVertices = [];
        private VertexPositionColor[] roadOutlineVertices = [];
        private RenderTarget2D? minimapFrame;
        private Rectangle destination;
        private bool isDisposed;
        private bool isLoaded;

        private static string BackgroundContentName => "Minimap";

        private static float ArtworkInteriorRadius => 470.0f;

        private static float ArtworkRimThickness => 30.0f;

        private static float ArtworkRoadLineWidth => 32.0f;

        private static float ArtworkOutlineWidth => 6.0f;

        private static float MarkerInradius => 3.0f / (1.0f + MathF.Sqrt(10.0f));

        private static float ViewportWidthRadiusRatio => 0.11305f;

        private static float ViewportHeightRadiusRatio => 0.1729f;

        private static float VisibleRoadRadius => 400.0f;

        private static int CircleSegmentCount => 128;

        private static int TriangleVertexCount => 3;

        private static int SupersamplingScale => 1;

        public MinimapRenderer(GraphicsDevice graphicsDevice)
            : this(graphicsDevice, Color.White)
        {
        }

        public void Load(IEnumerable<TrackRoutePoint> routePoints)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            ArgumentNullException.ThrowIfNull(routePoints);

            TrackRoutePoint[] route = [.. routePoints];
            roadVertices = [.. MinimapRoadVertexBuilder.Build(
                route,
                VisibleRoadRadius * ArtworkRoadLineWidth / ArtworkInteriorRadius)];
            roadOutlineVertices = [.. MinimapRoadVertexBuilder.Build(
                route,
                VisibleRoadRadius * (ArtworkRoadLineWidth + ArtworkOutlineWidth * 2.0f) /
                    ArtworkInteriorRadius)];

            if (isLoaded)
            {
                return;
            }

            guiManager.RegisterControl(background);
            guiManager.LoadContent();
            isLoaded = true;
        }

        public void Prepare(Matrix playerWorld, GameTime gameTime)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (!isLoaded)
            {
                return;
            }

            Viewport viewport = graphicsDevice.Viewport;
            float radius = MathF.Min(
                viewport.Width * ViewportWidthRadiusRatio,
                viewport.Height * ViewportHeightRadiusRatio);

            if (radius <= 0.0f)
            {
                return;
            }

            int diameter = (int)MathF.Ceiling(radius * 2.0f);
            float artworkScale = (float)diameter / background.SourceRectangle.Width;
            int margin = (int)(ArtworkRimThickness * artworkScale);
            destination = new Rectangle(margin, viewport.Height - diameter - margin, diameter, diameter);
            EnsureFrame(diameter * SupersamplingScale);
            RenderFrame(playerWorld, gameTime);
        }

        public void Draw()
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (minimapFrame is null)
            {
                return;
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
            spriteBatch.Draw(minimapFrame, destination, Color.White);
            spriteBatch.End();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            guiManager.UnloadContent();
            minimapFrame?.Dispose();
            spriteBatch.Dispose();
            effect.Dispose();
            rasterizerState.Dispose();
            maskBlendState.Dispose();
            maskWriteState.Dispose();
            maskReadState.Dispose();
            isDisposed = true;
        }

        private void EnsureFrame(int diameter)
        {
            if (minimapFrame is not null && minimapFrame.Width == diameter)
            {
                return;
            }

            minimapFrame?.Dispose();
            minimapFrame = new RenderTarget2D(
                graphicsDevice,
                diameter,
                diameter,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8);
        }

        private void RenderFrame(Matrix playerWorld, GameTime gameTime)
        {
            RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();
            Viewport previousViewport = graphicsDevice.Viewport;
            BlendState previousBlend = graphicsDevice.BlendState;
            DepthStencilState previousDepth = graphicsDevice.DepthStencilState;
            RasterizerState previousRasterizer = graphicsDevice.RasterizerState;

            try
            {
                graphicsDevice.SetRenderTarget(minimapFrame);
                graphicsDevice.Clear(Color.Transparent);
                Viewport viewport = graphicsDevice.Viewport;
                float radius = viewport.Width / 2.0f;
                float artworkScale = radius * 2.0f / background.SourceRectangle.Width;
                Vector2 centre = new(radius, radius);
                DrawBackground(centre, radius, gameTime);
                ConfigureProjection(viewport);
                DrawRoad(playerWorld, centre, ArtworkInteriorRadius * artworkScale);
                DrawMarker(centre, artworkScale);
            }
            finally
            {
                graphicsDevice.SetRenderTargets(previousTargets);
                graphicsDevice.Viewport = previousViewport;
                graphicsDevice.BlendState = previousBlend;
                graphicsDevice.DepthStencilState = previousDepth;
                graphicsDevice.RasterizerState = previousRasterizer;
            }
        }

        private void DrawBackground(Vector2 centre, float radius, GameTime gameTime)
        {
            background.Location = new Point2D((int)(centre.X - radius), (int)(centre.Y - radius));
            background.Size = new Size2D((int)(radius * 2.0f), (int)(radius * 2.0f));
            guiManager.Update(gameTime);
            spriteBatch.Begin();
            guiManager.Draw(spriteBatch);
            spriteBatch.End();
        }

        private void ConfigureProjection(Viewport viewport)
        {
            effect.View = Matrix.Identity;
            effect.Projection = Matrix.CreateOrthographicOffCenter(
                0.0f,
                viewport.Width,
                viewport.Height,
                0.0f,
                0.0f,
                1.0f);
            graphicsDevice.RasterizerState = rasterizerState;
        }

        private void DrawRoad(Matrix playerWorld, Vector2 centre, float radius)
        {
            graphicsDevice.Clear(ClearOptions.Stencil, Color.Transparent, 1.0f, 0);
            graphicsDevice.BlendState = maskBlendState;
            graphicsDevice.DepthStencilState = maskWriteState;
            effect.World = Matrix.CreateScale(radius) *
                Matrix.CreateTranslation(new Vector3(centre, 0.0f));
            DrawTriangles(maskVertices);
            graphicsDevice.BlendState = BlendState.AlphaBlend;
            graphicsDevice.DepthStencilState = maskReadState;
            effect.World = MinimapProjection.CreateTransform(playerWorld) *
                Matrix.CreateScale(radius / VisibleRoadRadius) *
                Matrix.CreateTranslation(new Vector3(centre, 0.0f));
            effect.DiffuseColor = Vector3.Zero;
            DrawTriangles(roadOutlineVertices);
            effect.DiffuseColor = Vector3.One;
            DrawTriangles(roadVertices);
        }

        private void DrawMarker(Vector2 centre, float artworkScale)
        {
            float markerScale = ArtworkRimThickness * artworkScale;
            float outlineScale = markerScale + ArtworkOutlineWidth * artworkScale / MarkerInradius;
            Vector3 incentre = new(0.0f, 1.0f - MarkerInradius, 0.0f);
            graphicsDevice.DepthStencilState = DepthStencilState.None;
            effect.World = Matrix.CreateTranslation(-incentre) *
                Matrix.CreateScale(outlineScale) *
                Matrix.CreateTranslation(new Vector3(centre, 0.0f) + incentre * markerScale);
            effect.DiffuseColor = Vector3.Zero;
            DrawTriangles(markerVertices);
            effect.World = Matrix.CreateScale(markerScale) *
                Matrix.CreateTranslation(new Vector3(centre, 0.0f));
            effect.DiffuseColor = Vector3.One;
            DrawTriangles(markerVertices);
        }

        private void DrawTriangles(VertexPositionColor[] vertices)
        {
            if (vertices.Length == 0)
            {
                return;
            }

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.TriangleList,
                    vertices,
                    0,
                    vertices.Length / TriangleVertexCount);
            }
        }

        private static VertexPositionColor[] CreateMaskVertices()
        {
            VertexPositionColor[] vertices = new VertexPositionColor[
                CircleSegmentCount * TriangleVertexCount];

            for (int segmentIndex = 0; segmentIndex < CircleSegmentCount; segmentIndex += 1)
            {
                float startAngle = MathHelper.TwoPi * segmentIndex / CircleSegmentCount;
                float endAngle = MathHelper.TwoPi * (segmentIndex + 1) / CircleSegmentCount;
                int vertexIndex = segmentIndex * TriangleVertexCount;
                vertices[vertexIndex] = new VertexPositionColor(Vector3.Zero, Color.White);
                vertices[vertexIndex + 1] = new VertexPositionColor(
                    new Vector3(MathF.Cos(startAngle), MathF.Sin(startAngle), 0.0f),
                    Color.White);
                vertices[vertexIndex + 2] = new VertexPositionColor(
                    new Vector3(MathF.Cos(endAngle), MathF.Sin(endAngle), 0.0f),
                    Color.White);
            }

            return vertices;
        }
    }
}