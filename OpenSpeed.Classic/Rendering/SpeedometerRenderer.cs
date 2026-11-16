using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSpeed.Classic.Rendering
{
    public sealed class SpeedometerRenderer(
        GraphicsDevice graphicsDevice,
        SpriteFont speedometerFont,
        Texture2D speedometerTexture) : IDisposable
    {
        private const float StartAngle = MathHelper.PiOver2;
        private const float EndAngle = MathHelper.TwoPi;

        private readonly BasicEffect effect = new(graphicsDevice)
        {
            TextureEnabled = false,
            VertexColorEnabled = true
        };
        private readonly RasterizerState rasterizerState = new()
        {
            CullMode = CullMode.None
        };
        private readonly SpriteBatch spriteBatch = new(graphicsDevice);

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            effect.Dispose();
            rasterizerState.Dispose();
            spriteBatch.Dispose();
            isDisposed = true;
        }

        public void Draw(float longitudinalVelocity)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            int viewportWidth = graphicsDevice.Viewport.Width;
            int viewportHeight = graphicsDevice.Viewport.Height;
            if (viewportWidth <= 0 || viewportHeight <= 0)
            {
                return;
            }

            SpeedometerReading reading = SpeedometerReadingCalculator.Calculate(
                longitudinalVelocity);
            float radius = MathF.Min(
                viewportWidth * 0.11305f,
                viewportHeight * 0.1729f);
            Vector2 centre = new(
                viewportWidth - radius - radius * 0.16f,
                viewportHeight - radius - radius * 0.12f);
            List<VertexPositionColor> lines = [];

            float needleAngle = StartAngle + (EndAngle - StartAngle) * reading.DialRatio;
            Vector2 needleEnd = centre + ToVector(needleAngle) * radius * 0.72f;
            AddLine(
                lines,
                centre - ToVector(needleAngle) * radius * 0.15f,
                needleEnd,
                new Color(225, 225, 220));

            effect.World = Matrix.Identity;
            effect.View = Matrix.Identity;
            effect.Projection = Matrix.CreateOrthographicOffCenter(
                0.0f,
                viewportWidth,
                viewportHeight,
                0.0f,
                0.0f,
                1.0f);
            graphicsDevice.BlendState = BlendState.AlphaBlend;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
            graphicsDevice.RasterizerState = rasterizerState;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(
                speedometerTexture,
                new Rectangle(
                    (int)(centre.X - radius),
                    (int)(centre.Y - radius),
                    (int)(radius * 2.0f),
                    (int)(radius * 2.0f)),
                Color.White);
            spriteBatch.End();
            DrawPrimitives(lines, PrimitiveType.LineList);
            DrawText(centre, radius, reading);
        }

        private void DrawText(
            Vector2 centre,
            float radius,
            SpeedometerReading reading)
        {
            string speedText = reading.SpeedKilometresPerHour.ToString();
            float speedScale = radius / 130.0f;
            Vector2 speedSize = speedometerFont.MeasureString(speedText) * speedScale;
            Vector2 speedPosition = centre + new Vector2(
                radius * 0.76f - speedSize.X,
                radius * 0.14f);
            Vector2 unitSize = speedometerFont.MeasureString("KPH") * speedScale * 0.42f;
            Vector2 unitPosition = centre + new Vector2(
                radius * 0.2f,
                radius * 0.56f);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawString(
                speedometerFont,
                speedText,
                speedPosition,
                new Color(235, 235, 225),
                0.0f,
                Vector2.Zero,
                speedScale,
                SpriteEffects.None,
                0.0f);
            spriteBatch.DrawString(
                speedometerFont,
                "KPH",
                unitPosition,
                new Color(190, 195, 195),
                0.0f,
                Vector2.Zero,
                speedScale * 0.42f,
                SpriteEffects.None,
                0.0f);
            spriteBatch.End();
        }

        private void DrawPrimitives(
            List<VertexPositionColor> vertices,
            PrimitiveType primitiveType)
        {
            if (vertices.Count == 0)
            {
                return;
            }

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(
                    primitiveType,
                    vertices.ToArray(),
                    0,
                    vertices.Count / (primitiveType == PrimitiveType.LineList ? 2 : 3));
            }
        }

        private static void AddLine(
            List<VertexPositionColor> vertices,
            Vector2 start,
            Vector2 end,
            Color colour)
        {
            vertices.Add(ToVertex(start, colour));
            vertices.Add(ToVertex(end, colour));
        }

        private static Vector2 ToVector(float angle)
            => new(MathF.Cos(angle), MathF.Sin(angle));

        private static VertexPositionColor ToVertex(Vector2 position, Color colour)
            => new(new Vector3(position, 0.0f), colour);
    }
}