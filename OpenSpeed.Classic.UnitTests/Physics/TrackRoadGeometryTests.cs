using NUnit.Framework;

using OpenSpeed.Classic.Physics;
using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class TrackRoadGeometryTests
    {
        [TestCase(2)]
        [TestCase(-2)]
        public void GivenNoRoadAtTheCar_WhenResolving_ThenCorridorClampingCannotCreateAContact(int horizontal)
        {
            byte[] records = new byte[PhysicsRoute.RecordSize * 2];
            records[0x0D] = 127;
            records[0x11] = 127;
            records[0x12] = 127;
            records[PhysicsRoute.RecordSize + 0x0D] = 127;
            records[PhysicsRoute.RecordSize + 0x11] = 127;
            records[PhysicsRoute.RecordSize + 0x12] = 127;
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x9C] = horizontal * X86Math.One;
            car[0xA8] = horizontal * X86Math.One;
            car[0x160] = 613;
            car[0xB8] = X86Math.One;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                            ProvisionalRouteContact.Resolve(car, new PhysicsRoute(records), BuildGeometry(0)),
                            Is.False);
                Assert.That(car[0x9C], Is.EqualTo(horizontal * X86Math.One));
                Assert.That(car[0xA8], Is.EqualTo(horizontal * X86Math.One));
                Assert.That(car[0x160], Is.EqualTo(613));
            }

        }

        [TestCase(0, 3000)]
        [TestCase(10000, 4000)]
        public void GivenAWideSlopedPolygon_WhenQuerying_ThenHeightUsesUnwrappedAssetCoordinates(
            int horizontal,
            int expectedHeight)
        {
            TrackRoadGeometry geometry = new(
            [
                new TrackBlock
                {
                    Surfaces =
                    [
                        new TrackSurface
                        {
                            DetailLevel = TrackGeometryDetailLevel.High,
                            Points =
                            [
                                new TrackPoint { X = -30000, Y = 0, Z = -4 },
                                new TrackPoint { X = 30000, Y = 6000, Z = -4 },
                                new TrackPoint { X = 30000, Y = 6000, Z = 4 },
                                new TrackPoint { X = -30000, Y = 0, Z = 4 }
                            ]
                        }
                    ]
                }
            ]);
            CarMemory car = new();
            car[0x9C] = horizontal * X86Math.One;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(geometry.TryApplyPlane(car, expectedHeight * X86Math.One));
                Assert.That(car[0x140], Is.EqualTo(expectedHeight * X86Math.One));
            }

        }

        [TestCase(0)]
        [TestCase(4)]
        public void GivenAValidRoadAndAZeroRouteNormal_WhenResolving_ThenPolygonGeometryStillSupportsTheCar(
            int height)
        {
            PhysicsRoute route = new(new byte[PhysicsRoute.RecordSize * 2]);
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0xA0] = height * X86Math.One;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ProvisionalRouteContact.Resolve(car, route, BuildGeometry(height)));
                Assert.That(car[0xA0], Is.EqualTo(height * X86Math.One));
                Assert.That(car[0x128], Is.GreaterThan(0x1999));
            }

        }

        [Test]
        public void GivenRepeatedQueries_WhenApplyingPlanes_ThenNormalsAndBoundaryResultsRemainStable()
        {
            TrackRoadGeometry geometry = BuildGeometry(4);
            CarMemory car = new();

            for (int iteration = 0; iteration < 32; iteration += 1)
            {
                car[0x9C] = X86Math.One;
                car[0xA4] = X86Math.One;
                Assert.Multiple(() =>
                {
                    Assert.That(geometry.TryApplyPlane(car, 4 * X86Math.One));
                    Assert.That(car[0x140], Is.EqualTo(4 * X86Math.One));
                    Assert.That(car[0x128], Is.GreaterThan(0x1999));
                });

                car[0x128] = 0;
                car[0x9C] = X86Math.One + 1;
                Assert.Multiple(() =>
                {
                    Assert.That(geometry.TryApplyPlane(car, 4 * X86Math.One), Is.False);
                    Assert.That(car[0x128], Is.Zero);
                });

            }
        }

        [TestCase(256)]
        [TestCase(1024)]
        public void GivenALargeRoadPolygon_WhenDetecting_ThenItsNormalDoesNotWrapToZero(int width)
        {
            TrackRoadGeometry geometry = new(
            [
                new TrackBlock
                {
                    Surfaces =
                    [
                        new TrackSurface
                        {
                            DetailLevel = TrackGeometryDetailLevel.High,
                            Points =
                            [
                                new TrackPoint { X = 0, Y = 4, Z = 0 },
                                new TrackPoint { X = width, Y = 4, Z = 0 },
                                new TrackPoint { X = 0, Y = 4, Z = width }
                            ]
                        }
                    ]
                }
            ]);
            CarMemory car = new();
            car[0x9C] = X86Math.One;
            car[0xA4] = -X86Math.One;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(geometry.TryApplyPlane(car, 4 * X86Math.One));
                Assert.That(car[0x140], Is.EqualTo(4 * X86Math.One));
                Assert.That(car[0x128], Is.GreaterThan(0x1999));
            }
        }

        [TestCase(0, true)]
        [TestCase(65536, true)]
        [TestCase(65537, false)]
        public void GivenAFiniteRoadPolygon_WhenQuerying_ThenTheEdgeIsInclusiveButThePlaneDoesNotExtendBeyondIt(
            int coordinate,
            bool expectsContact)
        {
            TrackRoadGeometry geometry = BuildGeometry(4);
            CarMemory car = new();
            car[0x9C] = coordinate;
            car[0xA0] = 42;
            car[0x13C] = 613;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(geometry.TryApplyPlane(car, 4 * X86Math.One), Is.EqualTo(expectsContact));
                Assert.That(car[0xA0], Is.EqualTo(42));
            }


            if (expectsContact)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(car[0x140], Is.EqualTo(4 * X86Math.One));
                    Assert.That(car[0x128], Is.GreaterThan(0x1999));
                }


                return;
            }

            Assert.That(car[0x13C], Is.EqualTo(613));
        }

        [TestCase(0, 0)]
        [TestCase(4, 4)]
        public void GivenStackedPolygons_WhenQuerying_ThenRouteHeightSelectsTheCorrectLevel(
            int expectedHeight,
            int selectedHeight)
        {
            TrackRoadGeometry geometry = BuildGeometry(0, 4);
            CarMemory car = new();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(geometry.TryApplyPlane(car, expectedHeight * X86Math.One));
                Assert.That(car[0x140], Is.EqualTo(selectedHeight * X86Math.One));
            }

        }

        [Test]
        public void GivenAPolygonAboveTheRoute_WhenResolvingContact_ThenTheDetectedPlaneControlsGroundHeight()
        {
            byte[] records = new byte[PhysicsRoute.RecordSize * 2];
            records[0x0D] = 127;
            records[0x11] = 127;
            records[0x12] = 127;
            records[PhysicsRoute.RecordSize + 0x0D] = 127;
            records[PhysicsRoute.RecordSize + 0x11] = 127;
            records[PhysicsRoute.RecordSize + 0x12] = 127;
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ProvisionalRouteContact.Resolve(car, new PhysicsRoute(records), BuildGeometry(4)));
                Assert.That(car[0xA0], Is.EqualTo(4 * X86Math.One));
            }

        }

        private static TrackRoadGeometry BuildGeometry(params int[] heights)
        {
            TrackSurface[] surfaces = new TrackSurface[heights.Length];

            for (int index = 0; index < heights.Length; index += 1)
            {
                surfaces[index] = new TrackSurface
                {
                    DetailLevel = TrackGeometryDetailLevel.High,
                    Points =
                    [
                        new TrackPoint { X = -1, Y = heights[index], Z = -1 },
                        new TrackPoint { X = 1, Y = heights[index], Z = -1 },
                        new TrackPoint { X = 1, Y = heights[index], Z = 1 },
                        new TrackPoint { X = -1, Y = heights[index], Z = 1 }
                    ]
                };
            }

            return new TrackRoadGeometry([new TrackBlock { Surfaces = surfaces }]);
        }
    }
}