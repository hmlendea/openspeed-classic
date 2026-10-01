using System;
using System.Linq;

using NUnit.Framework;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Cars
{
    [TestFixture]
    public sealed class CarTextureColourRemapperTests
    {
        [TestCase(CarIdentifier.McLarenF1)]
        [TestCase(CarIdentifier.ItaldesignNazcaC2)]
        [TestCase(CarIdentifier.FordMustangMachIII)]
        public void GivenPaintPixels_WhenExtractingTheColour_ThenItCorrespondsToTheRemappedPaint(
            CarIdentifier carIdentifier)
        {
            TrackColour sourceColour = new() { Red = 64, Green = 255, Blue = 64, Alpha = 128 };
            CarTexture texture = BuildTexture(sourceColour);
            TrackColour paintColour = CarTextureColourRemapper.GetPaintColour([texture], carIdentifier);

            Assert.That(texture.Pixels.Single(), Is.SameAs(sourceColour));

            CarTextureColourRemapper.Apply([texture], carIdentifier);
            TrackColour remappedColour = texture.Pixels.Single();

            Assert.Multiple(() =>
            {
                Assert.That(paintColour.Red, Is.EqualTo(remappedColour.Red));
                Assert.That(paintColour.Green, Is.EqualTo(remappedColour.Green));
                Assert.That(paintColour.Blue, Is.EqualTo(remappedColour.Blue));
                Assert.That(paintColour.Alpha, Is.EqualTo(byte.MaxValue));
                Assert.That(remappedColour.Alpha, Is.EqualTo(128));
            });
        }

        [Test]
        public void GivenShadowsAndTransparentPixels_WhenExtractingTheColour_ThenVisiblePaintIsSelected()
        {
            CarTexture[] textures = [
                BuildTexture(new TrackColour { Green = 64, Alpha = 255 }),
                BuildTexture(new TrackColour { Green = 255, Alpha = 0 }),
                BuildTexture(new TrackColour { Red = 255, Green = 255, Blue = 255, Alpha = 255 }),
                BuildTexture(new TrackColour { Red = 32, Green = 128, Blue = 32, Alpha = 255 })
            ];
            TrackColour paintColour = CarTextureColourRemapper.GetPaintColour(textures, CarIdentifier.McLarenF1);

            Assert.Multiple(() =>
            {
                Assert.That(paintColour.Red, Is.EqualTo(105));
                Assert.That(paintColour.Green, Is.EqualTo(26));
                Assert.That(paintColour.Blue, Is.EqualTo(26));
                Assert.That(paintColour.Alpha, Is.EqualTo(byte.MaxValue));
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GivenNoPaintPixels_WhenExtractingTheColour_ThenTheCatalogueColourIsUsed(bool hasTexture)
        {
            CarTexture[] textures = [];

            if (hasTexture)
            {
                textures = [BuildTexture(new TrackColour { Red = 255, Blue = 255, Alpha = 255 })];
            }

            TrackColour paintColour = CarTextureColourRemapper.GetPaintColour(textures, CarIdentifier.McLarenF1);
            TrackColour expected = CarColourCatalogue.GetTargetColour(CarIdentifier.McLarenF1);

            Assert.Multiple(() =>
            {
                Assert.That(paintColour.Red, Is.EqualTo(expected.Red));
                Assert.That(paintColour.Green, Is.EqualTo(expected.Green));
                Assert.That(paintColour.Blue, Is.EqualTo(expected.Blue));
                Assert.That(paintColour.Alpha, Is.EqualTo(expected.Alpha));
            });
        }

        [Test]
        public void GivenNullTextures_WhenExtractingTheColour_ThenAnExceptionIsThrown()
            => Assert.That(
                () => CarTextureColourRemapper.GetPaintColour(null!, CarIdentifier.McLarenF1),
                Throws.TypeOf<ArgumentNullException>());

        [Test]
        public void GivenAnInvalidCar_WhenExtractingTheColour_ThenAnExceptionIsThrown()
            => Assert.That(
                () => CarTextureColourRemapper.GetPaintColour([], (CarIdentifier)(-1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());

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

        [TestCase(0, 0, 0)]
        [TestCase(255, 255, 255)]
        [TestCase(128, 128, 128)]
        [TestCase(18, 171, 239)]
        [TestCase(124, 179, 66)]
        public void GivenACustomPaintColour_WhenApplying_ThenTheExactRgbAndSourceAlphaAreUsed(
            byte red,
            byte green,
            byte blue)
        {
            CarTexture texture = BuildTexture(new TrackColour { Red = 64, Green = 255, Blue = 64, Alpha = 128 });
            TrackColour targetColour = new() { Red = red, Green = green, Blue = blue, Alpha = 255 };

            CarTextureColourRemapper.Apply([texture], targetColour);
            TrackColour colour = texture.Pixels.Single();

            Assert.Multiple(() =>
            {
                Assert.That(colour.Red, Is.EqualTo(red));
                Assert.That(colour.Green, Is.EqualTo(green));
                Assert.That(colour.Blue, Is.EqualTo(blue));
                Assert.That(colour.Alpha, Is.EqualTo(128));
            });
        }

        [Test]
        public void GivenShadedPaintAndOtherDetails_WhenApplyingACustomColour_ThenShadingAndDetailsArePreserved()
        {
            TrackColour detail = new() { Red = 64, Green = 64, Blue = 64, Alpha = 255 };
            CarTexture texture = new()
            {
                Pixels = [new TrackColour { Green = 128, Alpha = 64 }, detail]
            };
            TrackColour targetColour = new() { Red = 64, Green = 128, Blue = 255, Alpha = 255 };

            CarTextureColourRemapper.Apply([texture], targetColour);
            TrackColour[] colours = [.. texture.Pixels];

            Assert.Multiple(() =>
            {
                Assert.That(colours[0].Red, Is.EqualTo(32));
                Assert.That(colours[0].Green, Is.EqualTo(64));
                Assert.That(colours[0].Blue, Is.EqualTo(128));
                Assert.That(colours[0].Alpha, Is.EqualTo(64));
                Assert.That(colours[1], Is.SameAs(detail));
            });
        }

        [Test]
        public void GivenNullTextures_WhenApplyingACustomColour_ThenAnExceptionIsThrown()
            => Assert.That(
                () => CarTextureColourRemapper.Apply(null!, new TrackColour()),
                Throws.TypeOf<ArgumentNullException>());

        [Test]
        public void GivenANullCustomColour_WhenApplying_ThenAnExceptionIsThrown()
            => Assert.That(
                () => CarTextureColourRemapper.Apply([], null!),
                Throws.TypeOf<ArgumentNullException>());

        private static CarTexture BuildTexture(TrackColour colour)
            => new()
            {
                Width = 1,
                Height = 1,
                Pixels = [colour]
            };
    }
}