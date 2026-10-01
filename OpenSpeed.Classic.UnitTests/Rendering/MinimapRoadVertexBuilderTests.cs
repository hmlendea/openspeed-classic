using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering
{
    [TestFixture]
    public sealed class MinimapRoadVertexBuilderTests
    {
        [Test]
        public void GivenNullRoutePoints_WhenConstructingTheRoad_ThenAnExceptionIsThrown()
            => Assert.That(
                () => MinimapRoadVertexBuilder.Build(null!),
                Throws.TypeOf<ArgumentNullException>());

        [TestCase(0)]
        [TestCase(1)]
        public void GivenInsufficientRoutePoints_WhenConstructingTheRoad_ThenNoTrianglesAreProduced(
            int pointCount)
        {
            TrackRoutePoint[] route = Enumerable.Range(0, pointCount)
                .Select(pointIndex => CreateRoutePoint(pointIndex)).ToArray();

            Assert.That(MinimapRoadVertexBuilder.Build(route), Is.Empty);
        }

        [TestCase(0.0f)]
        [TestCase(-1.0f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void GivenAnInvalidLineWidth_WhenConstructingTheRoad_ThenAnExceptionIsThrown(float lineWidth)
            => Assert.That(
                () => MinimapRoadVertexBuilder.Build([], lineWidth),
                Throws.TypeOf<ArgumentOutOfRangeException>());

        [TestCase(4.0f, 0.0f)]
        [TestCase(8.0f, 64.0f)]
        [TestCase(16.0f, -64.0f)]
        public void GivenDifferentRoadBoundaries_WhenConstructingTheRoad_ThenTheLineWidthIsConstant(
            float lineWidth,
            float lateralOffset)
        {
            TrackRoutePoint[] route = [CreateRoutePoint(0.0f), CreateRoutePoint(-64.0f)];
            route[1].Position.X += lateralOffset;
            route[1].Position.Y = 128.0f;
            route[1].LeftBorderDistance = 96.0f;
            route[1].RightBorderDistance = 256.0f;
            route[1].Right = new TrackVector { Y = 1.0f };
            VertexPositionColor[] vertices = [.. MinimapRoadVertexBuilder.Build(route, lineWidth)];

            Assert.Multiple(() =>
            {
                Assert.That(Vector3.Distance(vertices[0].Position, vertices[1].Position),
                    Is.EqualTo(lineWidth).Within(0.00001f));
                Assert.That(Vector3.Distance(vertices[2].Position, vertices[5].Position),
                    Is.EqualTo(lineWidth).Within(0.00001f));
                Assert.That((vertices[0].Position + vertices[1].Position) / 2.0f,
                    Is.EqualTo(new Vector3(42.0f, 0.0f, 0.0f)));
                Assert.That((vertices[2].Position + vertices[5].Position) / 2.0f,
                    Is.EqualTo(new Vector3(42.0f + lateralOffset, 0.0f, -64.0f)));
                Assert.That(vertices.Select(vertex => vertex.Position.Y), Is.All.Zero);
                Assert.That(vertices.Select(vertex => vertex.Color),
                    Is.All.EqualTo(new Color(210, 215, 215)));
            });
        }

        [Test]
        public void GivenACircuit_WhenConstructingTheRoad_ThenTheFinalPointConnectsToTheFirst()
        {
            TrackRoutePoint[] route = [
                CreateRoutePoint(0.0f), CreateRoutePoint(-64.0f), CreateRoutePoint(-128.0f)
            ];
            route[2].Position.X = 96.0f;
            VertexPositionColor[] vertices = [.. MinimapRoadVertexBuilder.Build(route)];
            int closingSegmentIndex = vertices.Length / route.Length * (route.Length - 1);

            Assert.Multiple(() =>
            {
                Assert.That(
                    (vertices[closingSegmentIndex].Position +
                        vertices[closingSegmentIndex + 1].Position) / 2.0f,
                    Is.EqualTo(new Vector3(96.0f, 0.0f, -128.0f)));
                Assert.That(
                    (vertices[closingSegmentIndex + 2].Position +
                        vertices[closingSegmentIndex + 5].Position) / 2.0f,
                    Is.EqualTo(new Vector3(42.0f, 0.0f, 0.0f)));
            });
        }

        [Test]
        public void GivenARouteCorner_WhenConstructingTheRoad_ThenTheJoinHasTheLineRadius()
        {
            VertexPositionColor[] vertices = [
                .. MinimapRoadVertexBuilder.Build([CreateRoutePoint(0.0f), CreateRoutePoint(-64.0f)])
            ];
            Vector3 centre = new(42.0f, 0.0f, 0.0f);
            VertexPositionColor[] joinVertices = vertices.Skip(6).Take(36).ToArray();

            for (int vertexIndex = 0; vertexIndex < joinVertices.Length; vertexIndex += 3)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(joinVertices[vertexIndex].Position, Is.EqualTo(centre));
                    Assert.That(Vector3.Distance(joinVertices[vertexIndex + 1].Position, centre),
                        Is.EqualTo(4.0f).Within(0.00001f));
                    Assert.That(Vector3.Distance(joinVertices[vertexIndex + 2].Position, centre),
                        Is.EqualTo(4.0f).Within(0.00001f));
                });
            }
        }

        [Test]
        public void GivenCoincidentRoutePoints_WhenConstructingTheRoad_ThenNoTrianglesAreProduced()
            => Assert.That(
                MinimapRoadVertexBuilder.Build([CreateRoutePoint(0.0f), CreateRoutePoint(0.0f)]),
                Is.Empty);

        [Test]
        public void GivenARepeatedRoutePoint_WhenConstructingTheRoad_ThenOtherSegmentsArePreserved()
        {
            VertexPositionColor[] expected = [
                .. MinimapRoadVertexBuilder.Build([CreateRoutePoint(0.0f), CreateRoutePoint(-64.0f)])
            ];
            VertexPositionColor[] vertices = [.. MinimapRoadVertexBuilder.Build([
                CreateRoutePoint(0.0f), CreateRoutePoint(0.0f), CreateRoutePoint(-64.0f)
            ])];

            Assert.That(vertices, Is.EqualTo(expected));
        }

        private static TrackRoutePoint CreateRoutePoint(float positionZ) => new()
        {
            Position = new TrackPoint { X = 42.0, Y = 8.0, Z = positionZ },
            Right = new TrackVector { X = 4.0 },
            Forward = new TrackVector { Z = -1.0 },
            LeftBorderDistance = 4.0,
            RightBorderDistance = 8.0
        };
    }
}