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
        [TestCase(1048575.0f, TrackGeometryDetailLevel.High)]
        [TestCase(1048576.0f, TrackGeometryDetailLevel.High)]
        [TestCase(1048577.0f, TrackGeometryDetailLevel.Medium)]
        [TestCase(4194303.0f, TrackGeometryDetailLevel.Medium)]
        [TestCase(4194304.0f, TrackGeometryDetailLevel.Medium)]
        [TestCase(4194305.0f, TrackGeometryDetailLevel.Low)]
        [TestCase(float.MaxValue, TrackGeometryDetailLevel.Low)]
        public void GivenHorizontalDistance_WhenSelecting_ThenTheVisibilityThresholdsApply(
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