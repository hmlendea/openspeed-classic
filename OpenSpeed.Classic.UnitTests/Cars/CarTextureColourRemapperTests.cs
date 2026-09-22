using System.Linq;

using NUnit.Framework;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Cars
{
    [TestFixture]
    public sealed class CarTextureColourRemapperTests
    {
        [Test]
        public void GivenGreenPaintPixels_WhenApplying_ThenTheyUseTheCarColour()
        {
            CarTexture texture = BuildTexture(new TrackColour
            {
                Red = 0,
                Green = byte.MaxValue,
                Blue = 0,
                Alpha = 128
            });

            CarTextureColourRemapper.Apply([texture], CarIdentifier.McLarenF1);

            TrackColour colour = texture.Pixels.Single();
            Assert.Multiple(() =>
            {
                Assert.That(colour.Red, Is.EqualTo(210));
                Assert.That(colour.Green, Is.Zero);
                Assert.That(colour.Blue, Is.Zero);
                Assert.That(colour.Alpha, Is.EqualTo(128));
            });
        }

        [Test]
        public void GivenNonPaintPixels_WhenApplying_ThenTheyArePreserved()
        {
            TrackColour expectedColour = new()
            {
                Red = 64,
                Green = 64,
                Blue = 64,
                Alpha = byte.MaxValue
            };
            CarTexture texture = BuildTexture(expectedColour);

            CarTextureColourRemapper.Apply([texture], CarIdentifier.McLarenF1);

            Assert.That(texture.Pixels.Single(), Is.SameAs(expectedColour));
        }

        private static CarTexture BuildTexture(TrackColour colour)
            => new()
            {
                Width = 1,
                Height = 1,
                Pixels = [colour]
            };
    }
}