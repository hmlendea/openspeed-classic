using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NuciXNA.DataAccess.Content;

using NUnit.Framework;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Rendering;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering
{
    [TestFixture]
    [NonParallelizable]
    [Explicit("Requires a graphical session and an OpenGL graphics device.")]
    public sealed class MinimapRendererTests
    {
        [TestCase(640, 480, CarIdentifier.McLarenF1)]
        [TestCase(1024, 768, CarIdentifier.McLarenF1)]
        [TestCase(640, 480, CarIdentifier.ItaldesignNazcaC2)]
        [TestCase(1024, 768, CarIdentifier.ItaldesignNazcaC2)]
        [TestCase(640, 480, CarIdentifier.FordMustangMachIII)]
        [TestCase(1024, 768, CarIdentifier.FordMustangMachIII)]
        public void GivenADiagonalRoute_WhenDrawingTheMinimap_ThenEdgesAreAntialiasedAndTheSceneIsPreserved(
            int viewportWidth,
            int viewportHeight,
            CarIdentifier carIdentifier)
        {
            using OpenSpeedClassicGame game = new();
            game.RunOneFrame();
            GraphicsDevice graphicsDevice = game.GraphicsDevice;
            NuciContentManager.Instance.LoadContent(game.Content, graphicsDevice);
            TrackColour paintColour = CarTextureColourRemapper.GetPaintColour([
                new CarTexture { Pixels = [new TrackColour { Green = 255, Alpha = 255 }] }
            ], carIdentifier);
            Color playerColour = new(paintColour.Red, paintColour.Green, paintColour.Blue);
            using MinimapRenderer renderer = new(graphicsDevice, playerColour);
            renderer.Load([
                new TrackRoutePoint { Position = new TrackPoint { X = -256.0, Z = -128.0 } },
                new TrackRoutePoint { Position = new TrackPoint { X = 256.0, Z = 128.0 } }
            ]);
            using RenderTarget2D scene = new(
                graphicsDevice,
                viewportWidth,
                viewportHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24Stencil8,
                0,
                RenderTargetUsage.PreserveContents);
            graphicsDevice.SetRenderTarget(scene);
            graphicsDevice.Clear(Color.Magenta);
            Viewport originalViewport = graphicsDevice.Viewport;
            renderer.Draw();
            renderer.Prepare(Matrix.Identity, new GameTime());

            Assert.Multiple(() =>
            {
                Assert.That(graphicsDevice.GetRenderTargets().Single().RenderTarget, Is.SameAs(scene));
                Assert.That(graphicsDevice.Viewport, Is.EqualTo(originalViewport));
            });

            renderer.Draw();
            graphicsDevice.SetRenderTarget(null);
            Color[] pixels = new Color[viewportWidth * viewportHeight];
            scene.GetData(pixels);
            int[] markerPixels = Enumerable.Range(0, pixels.Length)
                .Where(pixelIndex => pixels[pixelIndex] == playerColour).ToArray();

            Assert.That(markerPixels, Is.Not.Empty, "The marker must use the car's paint colour.");

            Rectangle markerArea = new(
                markerPixels.Min(pixelIndex => pixelIndex % viewportWidth),
                markerPixels.Min(pixelIndex => pixelIndex / viewportWidth),
                markerPixels.Max(pixelIndex => pixelIndex % viewportWidth) -
                    markerPixels.Min(pixelIndex => pixelIndex % viewportWidth) + 1,
                markerPixels.Max(pixelIndex => pixelIndex / viewportWidth) -
                    markerPixels.Min(pixelIndex => pixelIndex / viewportWidth) + 1);
            markerArea.Inflate(6, 6);

            Assert.Multiple(() =>
            {
                Assert.That(pixels.Take(viewportWidth), Is.All.EqualTo(Color.Magenta));
                Assert.That(pixels.Any(IsPartiallyCoveredRoadPixel));
                Assert.That(HasBlackOutline(pixels, viewportWidth, playerColour, Rectangle.Empty),
                    "The marker must have a pure black outline.");
                Assert.That(HasBlackOutline(pixels, viewportWidth, new Color(210, 215, 215), markerArea),
                    "The road must have a pure black outline away from the marker.");
            });

            renderer.Dispose();

            Assert.Multiple(() =>
            {
                Assert.That(() => renderer.Draw(), Throws.TypeOf<ObjectDisposedException>());
                Assert.That(
                    () => renderer.Prepare(Matrix.Identity, new GameTime()),
                    Throws.TypeOf<ObjectDisposedException>());
                Assert.That(() => renderer.Dispose(), Throws.Nothing);
            });
        }

        private static bool IsPartiallyCoveredRoadPixel(Color pixel)
            => pixel.R > 30 && pixel.R < 200 && pixel.R < pixel.G && pixel.G == pixel.B;

        private static bool HasBlackOutline(
            Color[] pixels,
            int width,
            Color fillColour,
            Rectangle excludedArea)
        {
            int height = pixels.Length / width;

            for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex += 1)
            {
                int column = pixelIndex % width;
                int row = pixelIndex / width;

                if (pixels[pixelIndex] != fillColour || excludedArea.Contains(column, row))
                {
                    continue;
                }

                for (int adjacentRow = Math.Max(0, row - 3);
                    adjacentRow <= Math.Min(height - 1, row + 3);
                    adjacentRow += 1)
                {
                    for (int adjacentColumn = Math.Max(0, column - 3);
                        adjacentColumn <= Math.Min(width - 1, column + 3);
                        adjacentColumn += 1)
                    {
                        if (pixels[adjacentRow * width + adjacentColumn] == Color.Black)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}