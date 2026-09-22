using System;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Tracks;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Tracks
{
    [TestFixture]
    public sealed class TrackLevelOfDetailSelectorTests
    {
        [TestCase(0.0f, TrackGeometryDetailLevel.High)]
        [TestCase(12099.0f, TrackGeometryDetailLevel.High)]
        [TestCase(12100.0f, TrackGeometryDetailLevel.High)]
        [TestCase(12101.0f, TrackGeometryDetailLevel.Medium)]
        [TestCase(22499.0f, TrackGeometryDetailLevel.Medium)]
        [TestCase(22500.0f, TrackGeometryDetailLevel.Medium)]
        [TestCase(22501.0f, TrackGeometryDetailLevel.Low)]
        [TestCase(float.MaxValue, TrackGeometryDetailLevel.Low)]
        public void GivenHorizontalDistance_WhenSelecting_ThenTheOriginalThresholdsApply(
            float horizontalDistanceSquared,
            TrackGeometryDetailLevel expectedDetailLevel)
            => Assert.That(
                TrackLevelOfDetailSelector.Select(horizontalDistanceSquared),
                Is.EqualTo(expectedDetailLevel));

        [TestCase(-1.0f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void GivenInvalidDistance_WhenSelecting_ThenTheDistanceIsRejected(
            float horizontalDistanceSquared)
            => Assert.That(
                () => TrackLevelOfDetailSelector.Select(horizontalDistanceSquared),
                Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}