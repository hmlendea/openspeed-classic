using System;
using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering;

namespace OpenSpeed.Classic.UnitTests.Rendering
{
    [TestFixture]
    public sealed class TextureMipmapGeneratorTests
    {
        [Test]
        public void GivenAFourByFourTexture_WhenGenerating_ThenEveryMipmapLevelIsReturned()
        {
            Color[] pixels = Enumerable.Repeat(Color.Red, 16).ToArray();

            TextureMipmapLevel[] levels = TextureMipmapGenerator
                .Generate(pixels, 4, 4)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(levels, Has.Length.EqualTo(3));
                Assert.That(levels.Select(level => level.Width), Is.EqualTo(new[] { 4, 2, 1 }));
                Assert.That(levels.Select(level => level.Height), Is.EqualTo(new[] { 4, 2, 1 }));
            });
        }

        [Test]
        public void GivenFourColours_WhenGenerating_ThenTheNextLevelContainsTheirAverage()
        {
            Color[] pixels =
            [
                new Color(0, 16, 32, 48),
                new Color(64, 80, 96, 112),
                new Color(128, 144, 160, 176),
                new Color(192, 208, 224, 240)
            ];

            TextureMipmapLevel[] levels = TextureMipmapGenerator
                .Generate(pixels, 2, 2)
                .ToArray();
            Color averagedColour = levels[1].Pixels.Single();

            Assert.That(averagedColour, Is.EqualTo(new Color(96, 112, 128, 144)));
        }

        [Test]
        public void GivenANonSquareTexture_WhenGenerating_ThenBothDimensionsReachOne()
        {
            Color[] pixels = Enumerable.Repeat(Color.Red, 8).ToArray();

            TextureMipmapLevel[] levels = TextureMipmapGenerator
                .Generate(pixels, 4, 2)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(levels.Select(level => level.Width), Is.EqualTo(new[] { 4, 2, 1 }));
                Assert.That(levels.Select(level => level.Height), Is.EqualTo(new[] { 2, 1, 1 }));
            });
        }

        [TestCase(0, 4, 16)]
        [TestCase(4, 0, 16)]
        [TestCase(4, 4, 8)]
        public void GivenInvalidTextureData_WhenGenerating_ThenInvalidDataIsReported(
            int width,
            int height,
            int pixelCount)
        {
            Color[] pixels = Enumerable.Repeat(Color.Red, pixelCount).ToArray();

            Assert.That(
                () => TextureMipmapGenerator.Generate(pixels, width, height),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void GivenNullPixels_WhenGenerating_ThenAnArgumentNullExceptionIsThrown()
            => Assert.That(
                () => TextureMipmapGenerator.Generate(null!, 4, 4),
                Throws.TypeOf<ArgumentNullException>());
    }
}