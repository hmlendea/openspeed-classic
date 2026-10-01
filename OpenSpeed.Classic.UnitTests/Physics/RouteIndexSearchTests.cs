using System;
using System.Buffers.Binary;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class RouteIndexSearchTests
    {
        [TestCase(0, 0, 0)]
        [TestCase(1, 0, 0)]
        [TestCase(-1, 1, 0)]
        [TestCase(65536, 16384, 4)]
        [TestCase(int.MinValue, 0, 0)]
        public void GivenNativeCoordinates_WhenCalculatingMetrics_ThenShiftAndOverflowSemanticsArePreserved(
            int coordinate, int expectedWalk, int expectedCandidate)
        {
            FixedVector point = new() { First = coordinate, Second = int.MaxValue };

            Assert.Multiple(() =>
            {
                Assert.That(RouteIndexSearch.CalculateWalkMetric(point, new FixedVector()), Is.EqualTo(expectedWalk));
                Assert.That(RouteIndexSearch.CalculateCandidateMetric(point, new FixedVector()), Is.EqualTo(expectedCandidate));
            });
        }

        [TestCase(0, 30, 3)]
        [TestCase(3, 0, 0)]
        [TestCase(2, 10, 1)]
        [TestCase(1, 15, 1)]
        [TestCase(2, 15, 2)]
        public void GivenACircularRoute_WhenSelecting_ThenWalkingWrapsAndTiesRetainTheCurrentRecord(
            int current, int coordinate, int expected)
        {
            PhysicsRoute route = CreateRoute([0, 10, 20, 30]);

            Assert.That(RouteIndexSearch.Select(route, new FixedVector { First = coordinate * X86Math.One }, current), Is.EqualTo(expected));
        }

        [Test]
        public void GivenADistantLocalMinimum_WhenSelecting_ThenCoarseFallbackRelocatesTheSearch()
        {
            PhysicsRoute route = CreateRoute([200, 210, 220, 230, 240, 250, 260, 270, 0, 280, 290, 300, 310, 320, 330, 210]);

            Assert.That(RouteIndexSearch.Select(route, new FixedVector(), 0), Is.EqualTo(8));
        }

        [Test]
        public void GivenOneRecord_WhenSelecting_ThenTheWalkTerminates()
            => Assert.That(RouteIndexSearch.Select(CreateRoute([0]), new FixedVector(), 0), Is.Zero);

        [TestCase(-1)]
        [TestCase(1)]
        public void GivenAnInvalidIndex_WhenSelecting_ThenItIsNotSilentlyClamped(int index)
            => Assert.That(() => RouteIndexSearch.Select(CreateRoute([0]), new FixedVector(), index), Throws.TypeOf<ArgumentOutOfRangeException>());

        private static PhysicsRoute CreateRoute(int[] coordinates)
        {
            byte[] records = new byte[coordinates.Length * PhysicsRoute.RecordSize];

            for (int index = 0; index < coordinates.Length; index += 1)
            {
                BinaryPrimitives.WriteInt32LittleEndian(records.AsSpan(index * PhysicsRoute.RecordSize), coordinates[index] * X86Math.One);
            }

            return new PhysicsRoute(records);
        }
    }
}