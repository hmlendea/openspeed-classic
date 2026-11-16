using System.Collections.Generic;

using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Rendering.Tracks;

namespace OpenSpeed.Classic.UnitTests.Rendering.Tracks
{
    [TestFixture]
    public sealed class TrackBlockVisibilitySelectorTests
    {
        [TestCase(-1, true)]
        [TestCase(4, true)]
        [TestCase(16, true)]
        [TestCase(42, false)]
        public void GivenAVisibilitySet_WhenChecking_ThenGlobalsAndMembersAreVisible(
            int blockIdentifier,
            bool expectedVisibility)
            => Assert.That(
                TrackBlockVisibilitySelector.IsVisible(
                    blockIdentifier,
                    new[] { 4, 16 }),
                Is.EqualTo(expectedVisibility));

        [Test]
        public void GivenBlockCentres_WhenSelecting_ThenTheNearestHorizontalBlockIsReturned()
        {
            KeyValuePair<int, Vector3>[] blockCentres =
            [
                new KeyValuePair<int, Vector3>(4, Vector3.Zero),
                new KeyValuePair<int, Vector3>(16, new Vector3(16.0f, 512.0f, 0.0f)),
                new KeyValuePair<int, Vector3>(42, new Vector3(32.0f, 0.0f, 0.0f))
            ];

            Assert.That(
                TrackBlockVisibilitySelector.SelectNearestBlock(
                    blockCentres,
                    new Vector3(15.0f, 0.0f, 0.0f)),
                Is.EqualTo(16));
        }
    }
}