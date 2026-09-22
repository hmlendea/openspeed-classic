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
                Assert.That(vertices[0].Color, Is.EqualTo(new Color(56, 80, 131)));
                Assert.That(vertices[1].Color, Is.EqualTo(new Color(56, 80, 131)));
                Assert.That(vertices[96].Position.Z, Is.EqualTo(-64.0f).Within(PositionTolerance));
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
                Assert.That(vertices, Has.Length.EqualTo(1536));
                Assert.That(vertices[0].Position.X, Is.Zero.Within(PositionTolerance));
                Assert.That(vertices[0].Position.Y, Is.EqualTo(24.0f));
                Assert.That(vertices[0].Position.Z, Is.EqualTo(64.0f));
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(Vector2.Zero));
                Assert.That(vertices[^2].Position.Y, Is.GreaterThan(64.0f));
                Assert.That(vertices[^2].Position.Z, Is.Zero.Within(PositionTolerance));
                Assert.That(vertices[^2].TextureCoordinate, Is.EqualTo(Vector2.Zero));
            });
        }

        [Test]
        public void GivenHorizonSettings_WhenBuildingThePanorama_ThenTheTextureWrapsOnce()
        {
            TrackHorizon horizon = BuildHorizon();

            VertexPositionColorTexture[] vertices = TrackHorizonVertexBuilder
                .BuildPanorama(horizon)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices, Has.Length.EqualTo(192));
                Assert.That(vertices[0].Position.Y, Is.EqualTo(24.0f));
                Assert.That(vertices[1].Position.Y, Is.EqualTo(40.0f));
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(Vector2.One));
                Assert.That(vertices[1].TextureCoordinate, Is.EqualTo(Vector2.UnitX));
                Assert.That(vertices[^1].TextureCoordinate, Is.EqualTo(Vector2.UnitY));
            });
        }

        private static TrackHorizon BuildHorizon()
            => new()
            {
                DomeHeightOffset = -100,
                DomeHeightScale = 250,
                FlatProjectionDistance = 1000,
                HorizonTextureBottomHeight = 16,
                HorizonTextureTopHeight = 32,
                IsMirrored = true,
                RingBaseHeight = 8,
                RingHeight = 16,
                RingRadius = 64,
                SkyColour = new TrackColour
                {
                    Alpha = byte.MaxValue,
                    Red = 56,
                    Green = 80,
                    Blue = 131
                },
                Colours =
                [
                    new TrackColour(),
                    new TrackColour(),
                    new TrackColour { Alpha = byte.MaxValue, Red = 8, Green = 16, Blue = 32 },
                    new TrackColour { Alpha = byte.MaxValue, Red = 16, Green = 32, Blue = 48 },
                    new TrackColour { Alpha = byte.MaxValue, Red = 96, Green = 128, Blue = 256 - 1 }
                ]
            };
    }
}
