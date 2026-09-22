using System.Linq;

using Microsoft.Xna.Framework.Graphics;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Tracks;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Tracks
{
    [TestFixture]
    public sealed class TrackRoadMarkingVertexBuilderTests
    {
        [Test]
        public void GivenTwoRoadMarkingPoints_WhenBuildingVertices_ThenARaisedRibbonIsCreated()
        {
            TrackRoadMarking roadMarking = new()
            {
                Points =
                [
                    new TrackPoint { X = 4.0, Y = 8.0, Z = 16.0 },
                    new TrackPoint { X = 4.0, Y = 8.0, Z = -16.0 }
                ]
            };

            VertexPositionColor[] vertices = TrackRoadMarkingVertexBuilder
                .Build(roadMarking)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(vertices, Has.Length.EqualTo(6));
                Assert.That(vertices[0].Position.X, Is.LessThan(4.0f));
                Assert.That(vertices[1].Position.X, Is.GreaterThan(4.0f));
                Assert.That(vertices[0].Position.Y, Is.GreaterThan(8.0f));
                Assert.That(vertices[2].Position.Z, Is.EqualTo(-16.0f));
                Assert.That(vertices[0].Color.A, Is.EqualTo(96));
            });
        }

        [Test]
        public void GivenDuplicateRoadMarkingPoints_WhenBuildingVertices_ThenNoRibbonIsCreated()
        {
            TrackRoadMarking roadMarking = new()
            {
                Points =
                [
                    new TrackPoint { X = 4.0, Y = 8.0, Z = 16.0 },
                    new TrackPoint { X = 4.0, Y = 8.0, Z = 16.0 }
                ]
            };

            Assert.That(
                TrackRoadMarkingVertexBuilder.Build(roadMarking),
                Is.Empty);
        }

        [Test]
        public void GivenANullRoadMarking_WhenBuildingVertices_ThenTheArgumentIsRejected()
            => Assert.That(
                () => TrackRoadMarkingVertexBuilder.Build(null!),
                Throws.ArgumentNullException);
    }
}
