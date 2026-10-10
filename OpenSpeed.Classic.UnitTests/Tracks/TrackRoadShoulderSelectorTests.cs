using NUnit.Framework;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Tracks
{
    [TestFixture]
    public sealed class TrackRoadShoulderSelectorTests
    {
        [Test]
        public void GivenALeftShoulder_WhenSelectingTheSurfaceSide_ThenLeftIsReturned()
            => Assert.That(
                TrackRoadShoulderSelector.Select(
                    BuildHorizontalSurface(-8.0, -4.0, 8.0),
                    [BuildRoutePoint()]),
                Is.EqualTo(TrackSurfaceSide.Left));

        [Test]
        public void GivenARightShoulder_WhenSelectingTheSurfaceSide_ThenRightIsReturned()
            => Assert.That(
                TrackRoadShoulderSelector.Select(
                    BuildHorizontalSurface(4.0, 8.0, 8.0),
                    [BuildRoutePoint()]),
                Is.EqualTo(TrackSurfaceSide.Right));

        [Test]
        public void GivenACentralRoadSurface_WhenSelectingTheSurfaceSide_ThenCentreIsReturned()
            => Assert.That(
                TrackRoadShoulderSelector.Select(
                    BuildHorizontalSurface(-4.0, 4.0, 8.0),
                    [BuildRoutePoint()]),
                Is.EqualTo(TrackSurfaceSide.Centre));

        [Test]
        public void GivenAnElevatedSurface_WhenSelectingTheSurfaceSide_ThenCentreIsReturned()
            => Assert.That(
                TrackRoadShoulderSelector.Select(
                    BuildHorizontalSurface(4.0, 8.0, 16.0),
                    [BuildRoutePoint()]),
                Is.EqualTo(TrackSurfaceSide.Centre));

        [Test]
        public void GivenAVerticalSurface_WhenSelectingTheSurfaceSide_ThenCentreIsReturned()
        {
            TrackSurface surface = new()
            {
                Points =
                [
                    new TrackPoint { X = 8.0, Y = 4.0, Z = -4.0 },
                    new TrackPoint { X = 8.0, Y = 16.0, Z = -4.0 },
                    new TrackPoint { X = 8.0, Y = 16.0, Z = 4.0 },
                    new TrackPoint { X = 8.0, Y = 4.0, Z = 4.0 }
                ]
            };

            Assert.That(
                TrackRoadShoulderSelector.Select(surface, [BuildRoutePoint()]),
                Is.EqualTo(TrackSurfaceSide.Centre));
        }

        [Test]
        public void GivenNoRoutePoints_WhenSelectingTheSurfaceSide_ThenCentreIsReturned()
            => Assert.That(
                TrackRoadShoulderSelector.Select(
                    BuildHorizontalSurface(4.0, 8.0, 8.0),
                    []),
                Is.EqualTo(TrackSurfaceSide.Centre));

        private static TrackSurface BuildHorizontalSurface(
            double minimumX,
            double maximumX,
            double positionY)
            => new()
            {
                Points =
                [
                    new TrackPoint { X = maximumX, Y = positionY, Z = -4.0 },
                    new TrackPoint { X = minimumX, Y = positionY, Z = -4.0 },
                    new TrackPoint { X = minimumX, Y = positionY, Z = 4.0 },
                    new TrackPoint { X = maximumX, Y = positionY, Z = 4.0 }
                ]
            };

        private static TrackRoutePoint BuildRoutePoint()
            => new()
            {
                Position = new TrackPoint { Y = 8.0 },
                Normal = new TrackVector { Y = 127.0 },
                Right = new TrackVector { X = 127.0 },
                LeftBorderDistance = 8.0,
                RightBorderDistance = 8.0
            };
    }
}
