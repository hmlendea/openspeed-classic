using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NUnit.Framework;

using OpenSpeed.Classic.Cars;
using OpenSpeed.Classic.Rendering.Cars;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class CarGeometryVertexBuilderTests
    {
        private static float PositionTolerance => 0.001f;

        [Test]
        public void GivenFixedPointGeometry_WhenBuildingColouredVertices_ThenCoordinatesAreConverted()
        {
            CarGeometryTriangle triangle = BuildTriangle();

            VertexPositionColor[] vertices = CarGeometryVertexBuilder
                .BuildColoured(triangle, Color.White)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices, Has.Length.EqualTo(3));
                Assert.That(vertices[0].Position.X, Is.EqualTo(1.03125f).Within(PositionTolerance));
                Assert.That(vertices[0].Position.Y, Is.EqualTo(3.125f).Within(PositionTolerance));
                Assert.That(vertices[0].Position.Z, Is.EqualTo(-2.0625f).Within(PositionTolerance));
            });
        }

        [Test]
        public void GivenTextureCorners_WhenBuildingTexturedVertices_ThenCoordinatesAreMapped()
        {
            VertexPositionColorTexture[] vertices = CarGeometryVertexBuilder
                .BuildTextured(BuildTriangle(), Color.White)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices[0].TextureCoordinate, Is.EqualTo(new Vector2(0.0f, 0.0f)));
                Assert.That(vertices[1].TextureCoordinate, Is.EqualTo(new Vector2(1.0f, 0.0f)));
                Assert.That(vertices[2].TextureCoordinate, Is.EqualTo(new Vector2(1.0f, 1.0f)));
            });
        }

        private static CarGeometryTriangle BuildTriangle()
            => new(
                65536,
                131072,
                196608,
                "CAR1",
                new CarGeometryVertex(8, 16, 32),
                new CarGeometryVertex(16, 32, 48),
                new CarGeometryVertex(32, 48, 64),
                0,
                1,
                2);
    }
}