using System.IO;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Tracks;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Tracks
{
    [TestFixture]
    public sealed class TrackSurfaceVertexBuilderTests
    {
        [Test]
        public void GivenATrackSurface_WhenBuildingTexturedVertices_ThenTheQuadIsTriangulated()
        {
            TrackSurface surface = BuildSurface();
            TrackMaterial material = new();

            VertexPositionColorTexture[] vertices = TrackSurfaceVertexBuilder
                .BuildTextured(surface, material)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices, Has.Length.EqualTo(6));
                Assert.That(vertices[0].Position, Is.EqualTo(new Vector3(4.0f, 8.0f, 16.0f)));
                Assert.That(vertices[1].Position, Is.EqualTo(new Vector3(32.0f, 8.0f, 16.0f)));
                Assert.That(vertices[2].Position, Is.EqualTo(new Vector3(32.0f, 8.0f, 48.0f)));
                Assert.That(vertices[3].Position, Is.EqualTo(vertices[0].Position));
                Assert.That(vertices[4].Position, Is.EqualTo(vertices[2].Position));
                Assert.That(vertices[5].Position, Is.EqualTo(new Vector3(4.0f, 8.0f, 48.0f)));
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(Vector2.UnitX));
                Assert.That(vertices[1].TextureCoordinate, Is.EqualTo(Vector2.Zero));
                Assert.That(vertices[2].TextureCoordinate, Is.EqualTo(Vector2.UnitY));
                Assert.That(vertices[5].TextureCoordinate, Is.EqualTo(Vector2.One));
            });
        }

        [Test]
        public void GivenPackedLighting_WhenBuildingVertices_ThenEachCornerUsesItsIntensity()
        {
            TrackSurface surface = BuildSurface();
            surface.LightingLevels = 0x6880;

            VertexPositionColor[] vertices = TrackSurfaceVertexBuilder
                .BuildColoured(surface)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices[0].Color.R, Is.EqualTo(14));
                Assert.That(vertices[1].Color.R, Is.EqualTo(20));
                Assert.That(vertices[2].Color.R, Is.EqualTo(27));
                Assert.That(vertices[5].Color.R, Is.EqualTo(34));
            });
        }

        [Test]
        public void GivenDisabledShadows_WhenBuildingTexturedVertices_ThenEveryCornerUsesFullIntensity()
        {
            TrackSurface surface = BuildSurface();
            surface.LightingLevels = 0;

            VertexPositionColorTexture[] vertices = TrackSurfaceVertexBuilder
                .BuildTextured(surface, new TrackMaterial(), false)
                .ToArray();

            Assert.That(vertices.Select(vertex => vertex.Color), Is.All.EqualTo(Color.White));
        }

        [Test]
        public void GivenDisabledShadows_WhenBuildingColouredVertices_ThenEveryCornerUsesTheSurfaceColour()
        {
            TrackSurface surface = BuildSurface();
            surface.LightingLevels = 0;

            VertexPositionColor[] vertices = TrackSurfaceVertexBuilder
                .BuildColoured(surface, false)
                .ToArray();

            Assert.That(
                vertices.Select(vertex => vertex.Color),
                Is.All.EqualTo(new Color(72, 88, 80)));
        }

        [Test]
        public void GivenALeftBackgroundWall_WhenBuildingTexturedVertices_ThenTheTextureOrientationIsPreserved()
        {
            TrackSurface surface = BuildSurface();
            surface.Points =
            [
                new TrackPoint { X = 4.0, Y = 8.0, Z = 16.0 },
                new TrackPoint { X = 4.0, Y = 48.0, Z = 16.0 },
                new TrackPoint { X = 4.0, Y = 48.0, Z = 44.0 },
                new TrackPoint { X = 4.0, Y = 8.0, Z = 44.0 }
            ];
            TrackMaterial material = new();

            VertexPositionColorTexture[] vertices = TrackSurfaceVertexBuilder
                .BuildTextured(surface, material)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(Vector2.UnitX));
                Assert.That(vertices[1].TextureCoordinate, Is.EqualTo(Vector2.Zero));
                Assert.That(vertices[2].TextureCoordinate, Is.EqualTo(Vector2.UnitY));
                Assert.That(vertices[5].TextureCoordinate, Is.EqualTo(Vector2.One));
            });
        }

        [Test]
        public void GivenASlopedLeftBackgroundWall_WhenBuildingTexturedVertices_ThenTheTextureOrientationIsPreserved()
        {
            TrackSurface surface = BuildSurface();
            surface.Points =
            [
                new TrackPoint { X = 4.0, Y = 8.0, Z = 16.0 },
                new TrackPoint { X = 8.0, Y = 36.0, Z = 16.0 },
                new TrackPoint { X = 8.0, Y = 36.0, Z = 44.0 },
                new TrackPoint { X = 4.0, Y = 8.0, Z = 44.0 }
            ];
            TrackMaterial material = new();

            VertexPositionColorTexture[] vertices = TrackSurfaceVertexBuilder
                .BuildTextured(surface, material)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(Vector2.UnitX));
                Assert.That(vertices[1].TextureCoordinate, Is.EqualTo(Vector2.Zero));
                Assert.That(vertices[2].TextureCoordinate, Is.EqualTo(Vector2.UnitY));
                Assert.That(vertices[5].TextureCoordinate, Is.EqualTo(Vector2.One));
            });
        }

        [Test]
        public void GivenARightBackgroundWall_WhenBuildingTexturedVertices_ThenTheTextureIsRotatedByAHalfTurn()
        {
            TrackSurface surface = BuildSurface();
            surface.Points =
            [
                new TrackPoint { X = 4.0, Y = 48.0, Z = 16.0 },
                new TrackPoint { X = 4.0, Y = 8.0, Z = 16.0 },
                new TrackPoint { X = 4.0, Y = 8.0, Z = 44.0 },
                new TrackPoint { X = 4.0, Y = 48.0, Z = 44.0 }
            ];
            TrackMaterial material = new();

            VertexPositionColorTexture[] vertices = TrackSurfaceVertexBuilder
                .BuildTextured(surface, material)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(Vector2.UnitY));
                Assert.That(vertices[1].TextureCoordinate, Is.EqualTo(Vector2.One));
                Assert.That(vertices[2].TextureCoordinate, Is.EqualTo(Vector2.UnitX));
                Assert.That(vertices[5].TextureCoordinate, Is.EqualTo(Vector2.Zero));
            });
        }

        [Test]
        public void GivenAPlacedSceneryWall_WhenBuildingTexturedVertices_ThenTheTextureOrientationIsPreserved()
        {
            TrackSurface surface = BuildSurface();
            surface.Group = TrackSurfaceGroup.Unrestricted;
            surface.Points =
            [
                new TrackPoint { X = 4.0, Y = 8.0, Z = 16.0 },
                new TrackPoint { X = 32.0, Y = 8.0, Z = 16.0 },
                new TrackPoint { X = 32.0, Y = 48.0, Z = 16.0 },
                new TrackPoint { X = 4.0, Y = 48.0, Z = 16.0 }
            ];
            TrackMaterial material = new();

            VertexPositionColorTexture[] vertices = TrackSurfaceVertexBuilder
                .BuildTextured(surface, material)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(Vector2.UnitX));
                Assert.That(vertices[1].TextureCoordinate, Is.EqualTo(Vector2.Zero));
                Assert.That(vertices[2].TextureCoordinate, Is.EqualTo(Vector2.UnitY));
                Assert.That(vertices[5].TextureCoordinate, Is.EqualTo(Vector2.One));
            });
        }

        [Test]
        public void GivenARightRoadShoulder_WhenBuildingTexturedVertices_ThenTheTextureIsReflectedAcrossTheLateralAxis()
        {
            TrackSurface surface = BuildSurfaceMatchingWallHeuristic();
            surface.Side = TrackSurfaceSide.Right;
            TrackMaterial material = new() { Alignment = 0x0900 };

            VertexPositionColorTexture[] vertices = TrackSurfaceVertexBuilder
                .BuildTextured(surface, material)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(Vector2.UnitY));
                Assert.That(vertices[1].TextureCoordinate, Is.EqualTo(Vector2.Zero));
                Assert.That(vertices[2].TextureCoordinate, Is.EqualTo(Vector2.UnitX));
                Assert.That(vertices[5].TextureCoordinate, Is.EqualTo(Vector2.One));
            });
        }

        [Test]
        public void GivenALeftRoadShoulder_WhenBuildingTexturedVertices_ThenTheTextureOrientationIsPreserved()
        {
            TrackSurface surface = BuildSurfaceMatchingWallHeuristic();
            surface.Side = TrackSurfaceSide.Left;
            TrackMaterial material = new();

            VertexPositionColorTexture[] vertices = TrackSurfaceVertexBuilder
                .BuildTextured(surface, material)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(Vector2.UnitX));
                Assert.That(vertices[1].TextureCoordinate, Is.EqualTo(Vector2.Zero));
                Assert.That(vertices[2].TextureCoordinate, Is.EqualTo(Vector2.UnitY));
                Assert.That(vertices[5].TextureCoordinate, Is.EqualTo(Vector2.One));
            });
        }

        [Test]
        public void GivenAnIncompleteSurface_WhenBuildingVertices_ThenInvalidDataIsReported()
        {
            TrackSurface surface = new()
            {
                Identifier = 42,
                Points = [new TrackPoint()]
            };

            Assert.That(
                () => TrackSurfaceVertexBuilder.BuildColoured(surface),
                Throws.TypeOf<InvalidDataException>());
        }

        private static TrackSurface BuildSurface()
            => new()
            {
                Identifier = 42,
                LightingLevels = ushort.MaxValue,
                Points =
                [
                    new TrackPoint { X = 4.0, Y = 8.0, Z = 16.0 },
                    new TrackPoint { X = 32.0, Y = 8.0, Z = 16.0 },
                    new TrackPoint { X = 32.0, Y = 8.0, Z = 48.0 },
                    new TrackPoint { X = 4.0, Y = 8.0, Z = 48.0 }
                ]
            };

        private static TrackSurface BuildSurfaceMatchingWallHeuristic()
        {
            TrackSurface surface = BuildSurface();
            surface.Points =
            [
                new TrackPoint { X = 4.0, Y = 48.0, Z = 16.0 },
                new TrackPoint { X = 4.0, Y = 8.0, Z = 16.0 },
                new TrackPoint { X = 4.0, Y = 8.0, Z = 44.0 },
                new TrackPoint { X = 4.0, Y = 48.0, Z = 44.0 }
            ];

            return surface;
        }
    }
}
