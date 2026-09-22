using System.Linq;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Tracks;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Tracks
{
    [TestFixture]
    public sealed class TrackTextureCoordinateBuilderTests
    {
        [TestCase(0x0100, 0, 1, 2, 3)]
        [TestCase(0x0300, 3, 0, 1, 2)]
        [TestCase(0x0500, 1, 3, 0, 2)]
        [TestCase(0x0900, 3, 0, 1, 2)]
        [TestCase(0x1000, 2, 1, 3, 0)]
        [TestCase(0x1200, 3, 0, 2, 1)]
        [TestCase(0x1400, 1, 0, 2, 3)]
        [TestCase(0x1800, 3, 0, 2, 1)]
        public void GivenAnObservedOrientation_WhenBuildingTextureCoordinates_ThenEachCornerIsMapped(
            int alignment,
            int firstCorner,
            int secondCorner,
            int thirdCorner,
            int fourthCorner)
        {
            Vector2[] sourceCoordinates =
            [
                Vector2.UnitX,
                Vector2.Zero,
                Vector2.UnitY,
                Vector2.One
            ];
            TrackMaterial material = new()
            {
                Alignment = (ushort)alignment
            };

            Vector2[] textureCoordinates = TrackTextureCoordinateBuilder
                .Build(material)
                .ToArray();

            Assert.That(
                textureCoordinates,
                Is.EqualTo(new[]
                {
                    sourceCoordinates[firstCorner],
                    sourceCoordinates[secondCorner],
                    sourceCoordinates[thirdCorner],
                    sourceCoordinates[fourthCorner]
                }));
        }

        [Test]
        public void GivenTextureScrollBits_WhenBuildingTextureCoordinates_ThenOrientationIsUnchanged()
        {
            TrackMaterial unscrolledMaterial = new() { Alignment = 0x0300 };
            TrackMaterial scrolledMaterial = new() { Alignment = 0x0334 };

            Assert.That(
                TrackTextureCoordinateBuilder.Build(scrolledMaterial),
                Is.EqualTo(TrackTextureCoordinateBuilder.Build(unscrolledMaterial)));
        }

    }
}
