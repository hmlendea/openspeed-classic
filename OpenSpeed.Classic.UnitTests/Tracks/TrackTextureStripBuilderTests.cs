using System.Linq;

using NUnit.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Tracks
{
    [TestFixture]
    public sealed class TrackTextureStripBuilderTests
    {
        [Test]
        public void GivenAdjacentTextures_WhenBuildingAStrip_ThenPixelsAreJoinedHorizontally()
        {
            TrackColour firstColour = new() { Alpha = byte.MaxValue, Red = 16 };
            TrackColour secondColour = new() { Alpha = byte.MaxValue, Blue = 32 };
            TrackTexture[] sourceTextures =
            [
                new TrackTexture
                {
                    Identifier = 4,
                    Width = 1,
                    Height = 1,
                    Pixels = [firstColour]
                },
                new TrackTexture
                {
                    Identifier = 8,
                    Width = 1,
                    Height = 1,
                    Pixels = [secondColour]
                }
            ];

            TrackTexture texture = TrackTextureStripBuilder.Build(
                16,
                "HorizonPanorama",
                sourceTextures);
            TrackColour[] pixels = texture.Pixels.ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(texture.Identifier, Is.EqualTo(16));
                Assert.That(texture.Name, Is.EqualTo("HorizonPanorama"));
                Assert.That(texture.Width, Is.EqualTo(2));
                Assert.That(texture.Height, Is.EqualTo(1));
                Assert.That(pixels, Is.EqualTo(new[] { firstColour, secondColour }));
            });
        }

        [Test]
        public void GivenTexturesWithDifferentHeights_WhenBuildingAStrip_ThenTheInputIsRejected()
        {
            TrackTexture[] sourceTextures =
            [
                new TrackTexture { Width = 1, Height = 1, Pixels = [new TrackColour()] },
                new TrackTexture
                {
                    Width = 1,
                    Height = 2,
                    Pixels = [new TrackColour(), new TrackColour()]
                }
            ];

            Assert.That(
                () => TrackTextureStripBuilder.Build(
                    16,
                    "HorizonPanorama",
                    sourceTextures),
                Throws.ArgumentException);
        }

        [Test]
        public void GivenAdjacentTextures_WhenBuildingAMirroredStrip_ThenTheSecondHalfIsReflected()
        {
            TrackColour firstColour = new() { Alpha = byte.MaxValue, Red = 16 };
            TrackColour secondColour = new() { Alpha = byte.MaxValue, Blue = 32 };
            TrackTexture[] sourceTextures =
            [
                new TrackTexture
                {
                    Width = 1,
                    Height = 1,
                    Pixels = [firstColour]
                },
                new TrackTexture
                {
                    Width = 1,
                    Height = 1,
                    Pixels = [secondColour]
                }
            ];

            TrackTexture texture = TrackTextureStripBuilder.BuildMirrored(
                16,
                "HorizonPanorama",
                sourceTextures);

            Assert.That(
                texture.Pixels,
                Is.EqualTo(new[]
                {
                    firstColour,
                    secondColour,
                    secondColour,
                    firstColour
                }));
        }
    }
}
