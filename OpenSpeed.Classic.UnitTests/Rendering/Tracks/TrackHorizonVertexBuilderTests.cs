using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Tracks;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Tracks
{
    [TestFixture]
    public sealed class TrackHorizonVertexBuilderTests
    {
        private static float PositionTolerance => 0.001f;

        [Test]
        public void GivenHorizonSettings_WhenBuildingTheRing_ThenTheCircularBandIsGenerated()
        {
            TrackHorizon horizon = BuildHorizon();

            VertexPositionColor[] vertices = TrackHorizonVertexBuilder
                .BuildRing(horizon)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices, Has.Length.EqualTo(192));
                Assert.That(vertices[0].Position.X, Is.Zero.Within(PositionTolerance));
                Assert.That(vertices[0].Position.Y, Is.EqualTo(8.0f));
                Assert.That(vertices[0].Position.Z, Is.EqualTo(64.0f));
                Assert.That(vertices[1].Position.Y, Is.EqualTo(24.0f));
                Assert.That(vertices[0].Color, Is.EqualTo(new Color(16, 32, 48)));
            });
        }

        [Test]
        public void GivenHorizonSettings_WhenBuildingTheDome_ThenEveryGridCellIsTriangulated()
        {
            TrackHorizon horizon = BuildHorizon();

            VertexPositionColorTexture[] vertices = TrackHorizonVertexBuilder
                .BuildDome(horizon)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices, Has.Length.EqualTo(864));
                Assert.That(vertices[0].Position.X, Is.EqualTo(-1000.0f));
                Assert.That(vertices[0].Position.Z, Is.EqualTo(-1000.0f));
                Assert.That(vertices[0].TextureCoordinate.X, Is.EqualTo(1.0f));
                Assert.That(vertices[0].TextureCoordinate.Y, Is.Zero);
            });
        }

        private static TrackHorizon BuildHorizon()
            => new()
            {
                DomeHeightOffset = -100,
                DomeHeightScale = 250,
                IsMirrored = true,
                RingBaseHeight = 8,
                RingHeight = 16,
                RingRadius = 64,
                Colours =
                [
                    new TrackColour(),
                    new TrackColour(),
                    new TrackColour(),
                    new TrackColour { Alpha = byte.MaxValue, Red = 16, Green = 32, Blue = 48 },
                    new TrackColour { Alpha = byte.MaxValue, Red = 96, Green = 128, Blue = 256 - 1 }
                ]
            };
    }
}