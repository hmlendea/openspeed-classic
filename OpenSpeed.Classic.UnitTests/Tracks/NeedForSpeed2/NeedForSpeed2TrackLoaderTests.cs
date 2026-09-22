using System;
using System.IO;
using System.Linq;

using Moq;

using NuciLog.Core;

using NUnit.Framework;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Tracks;
using OpenSpeed.Classic.Tracks.NeedForSpeed2;

namespace OpenSpeed.Classic.UnitTests.Tracks.NeedForSpeed2
{
    [TestFixture]
    public sealed class NeedForSpeed2TrackLoaderTests
    {
        private NeedForSpeed2TrackLoader trackLoader = null!;
        private string testDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            testDirectory = Path.Combine(
                Path.GetTempPath(),
                $"openspeed-nfs2-{Guid.NewGuid():N}");
            Directory.CreateDirectory(testDirectory);
            NeedForSpeed2TrackFixture.Write(testDirectory);
            Mock<ILogger> logger = new();
            trackLoader = new NeedForSpeed2TrackLoader(
                new FilePathResolver(logger.Object));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }

        [Test]
        public void GivenACompleteNfs2Installation_WhenLoadingOutback_ThenTrackAssetsAreDecoded()
        {
            LoadedTrack track = trackLoader.Load(
                testDirectory,
                "outback",
                TrackTextureVariant.SE);
            TrackBlock block = track.Blocks.Single();
            TrackSurface surface = block.Surfaces.Single();
            TrackSurface[] blockScenery = block.ScenerySurfaces.ToArray();
            TrackSurface globalScenery = track.ScenerySurfaces.Single();
            TrackRoadMarking roadMarking = block.RoadMarkings.Single();
            TrackRoutePoint routePoint = track.RoutePoints.Single();
            TrackPoint[] roadMarkingPoints = roadMarking.Points.ToArray();
            TrackPoint[] points = surface.Points.ToArray();
            TrackMaterial material = track.Materials.Single();
            TrackTexture texture = track.Textures.Single(
                trackTexture => trackTexture.Identifier == 0);
            TrackHorizon horizon = track.Horizon!;
            TrackColour pixel = texture.Pixels.Single();

            Assert.Multiple(() =>
            {
                Assert.That(track.Identifier, Is.EqualTo("Outback"));
                Assert.That(track.DisplayName, Is.EqualTo("Outback"));
                Assert.That(track.Game, Is.EqualTo(GameVersion.NeedForSpeed2SpecialEdition));
                Assert.That(track.SourceFiles.Count(), Is.EqualTo(5));
                Assert.That(horizon.RingRadius, Is.EqualTo(1500));
                Assert.That(horizon.SkyColour.Red, Is.EqualTo(56));
                Assert.That(horizon.SkyColour.Green, Is.EqualTo(80));
                Assert.That(horizon.SkyColour.Blue, Is.EqualTo(131));
                Assert.That(horizon.PanoramaTexture, Is.Null);
                Assert.That(block.Identifier, Is.Zero);
                Assert.That(block.Centre.X, Is.EqualTo(4.0));
                Assert.That(block.Centre.Y, Is.EqualTo(8.0));
                Assert.That(block.Centre.Z, Is.EqualTo(-16.0));
                Assert.That(roadMarking.Identifier, Is.Zero);
                Assert.That(roadMarkingPoints, Has.Length.EqualTo(2));
                Assert.That(roadMarkingPoints[0].X, Is.EqualTo(4.0));
                Assert.That(roadMarkingPoints[1].Z, Is.EqualTo(-17.0));
                Assert.That(surface.DetailLevel, Is.EqualTo(TrackGeometryDetailLevel.High));
                Assert.That(surface.Group, Is.EqualTo(TrackSurfaceGroup.Primary));
                Assert.That(surface.MaterialIdentifier, Is.Zero);
                Assert.That(surface.LightingLevels, Is.EqualTo(ushort.MaxValue));
                Assert.That(points, Has.Length.EqualTo(4));
                Assert.That(points[2].X, Is.EqualTo(5.0));
                Assert.That(points[2].Z, Is.EqualTo(-17.0));
                Assert.That(blockScenery, Has.Length.EqualTo(2));
                Assert.That(
                    blockScenery[0].Points.First().X,
                    Is.EqualTo(6.0));
                Assert.That(
                    blockScenery[1].Points.First().X,
                    Is.EqualTo(8.0));
                Assert.That(blockScenery[0].Group, Is.EqualTo(TrackSurfaceGroup.Unrestricted));
                Assert.That(block.VisibleBlockIdentifiers, Is.EqualTo(new[] { 0, 42 }));
                Assert.That(globalScenery.Points.First().X, Is.EqualTo(10.0));
                Assert.That(routePoint.BlockIdentifier, Is.Zero);
                Assert.That(routePoint.Position.X, Is.EqualTo(4.0));
                Assert.That(routePoint.Position.Y, Is.EqualTo(8.0));
                Assert.That(routePoint.Position.Z, Is.EqualTo(-16.0));
                Assert.That(routePoint.Normal.Y, Is.EqualTo(127.0));
                Assert.That(routePoint.Forward.Z, Is.EqualTo(-127.0));
                Assert.That(routePoint.Right.X, Is.EqualTo(127.0));
                Assert.That(routePoint.LeftBorderDistance, Is.EqualTo(7.9375));
                Assert.That(routePoint.RightBorderDistance, Is.EqualTo(7.9375));
                Assert.That(material.Identifier, Is.Zero);
                Assert.That(material.TextureIdentifier, Is.Zero);
                Assert.That(material.Alignment, Is.EqualTo(0x0401));
                Assert.That(material.AnimationFrameCount, Is.EqualTo(32));
                Assert.That(material.AnimationFrameInterval, Is.EqualTo(48));
                Assert.That(texture.Identifier, Is.Zero);
                Assert.That(texture.Name, Is.EqualTo("TEST"));
                Assert.That(texture.Width, Is.EqualTo(1));
                Assert.That(texture.Height, Is.EqualTo(1));
                Assert.That(pixel.Red, Is.EqualTo(48));
                Assert.That(pixel.Green, Is.EqualTo(32));
                Assert.That(pixel.Blue, Is.EqualTo(16));
                Assert.That(pixel.Alpha, Is.EqualTo(byte.MaxValue));
            });
        }

        [Test]
        public void GivenPcTextures_WhenLoadingOutback_ThenThePcTextureArchiveIsDecoded()
        {
            LoadedTrack track = trackLoader.Load(
                testDirectory,
                "Outback",
                TrackTextureVariant.PC);
            TrackTexture texture = track.Textures.Single(
                trackTexture => trackTexture.Identifier == 0);
            TrackAssetFile textureSource = track.SourceFiles.Single(
                sourceFile => Equals(sourceFile.Role, TrackAssetRole.Textures));

            Assert.Multiple(() =>
            {
                Assert.That(texture.Name, Is.EqualTo("PCTX"));
                Assert.That(
                    textureSource.Path,
                    Is.EqualTo(Path.Combine(
                        testDirectory,
                        NeedForSpeed2TrackFixture.PcTextureRelativePath)));
            });
        }

        [Test]
        public void GivenAPartialOverride_WhenLoadingOutback_ThenOverrideAndRootAssetsAreCombined()
        {
            string overridesDirectory = Path.Combine(testDirectory, "Overrides");
            string geometryRelativePath = NeedForSpeed2TrackFixture.GeometryRelativePath;
            string textureRelativePath = NeedForSpeed2TrackFixture.TextureRelativePath;
            string overrideGeometryPath = Path.Combine(
                overridesDirectory,
                geometryRelativePath);
            string? overrideGeometryDirectory = Path.GetDirectoryName(overrideGeometryPath);
            Directory.CreateDirectory(overrideGeometryDirectory!);
            Mock<ILogger> logger = new();
            IFilePathResolver filePathResolver = new FilePathResolver(logger.Object);
            string rootGeometryPath = filePathResolver.ResolveFile(
                testDirectory,
                geometryRelativePath)!;
            string rootTexturePath = filePathResolver.ResolveFile(
                testDirectory,
                textureRelativePath)!;
            File.Copy(
                rootGeometryPath,
                overrideGeometryPath);

            LoadedTrack track = trackLoader.Load(
                testDirectory,
                overridesDirectory,
                "Outback",
                TrackTextureVariant.SE);
            TrackAssetFile geometrySource = track.SourceFiles.Single(
                sourceFile => Equals(sourceFile.Role, TrackAssetRole.Geometry));
            TrackAssetFile textureSource = track.SourceFiles.Single(
                sourceFile => Equals(sourceFile.Role, TrackAssetRole.Textures));

            Assert.Multiple(() =>
            {
                Assert.That(geometrySource.Path, Is.EqualTo(overrideGeometryPath));
                Assert.That(
                    textureSource.Path,
                    Is.EqualTo(rootTexturePath));
            });
        }

        [Test]
        public void GivenAMissingInstallation_WhenLoadingATrack_ThenDirectoryNotFoundIsReported()
        {
            string missingDirectory = Path.Combine(testDirectory, "Absent");

            Assert.That(
                () => trackLoader.Load(missingDirectory, "Outback"),
                Throws.TypeOf<DirectoryNotFoundException>());
        }

        [Test]
        public void GivenAnUnsupportedIdentifier_WhenLoadingATrack_ThenTheIdentifierIsRejected()
            => Assert.That(
                () => trackLoader.Load(testDirectory, "Minecraft"),
                Throws.TypeOf<ArgumentException>());

        [Test]
        public void GivenAMissingMaterialFile_WhenLoadingATrack_ThenTheAssetIsReportedMissing()
        {
            File.Delete(Path.Combine(testDirectory, NeedForSpeed2TrackFixture.MaterialRelativePath));

            Assert.That(
                () => trackLoader.Load(testDirectory, "Outback"),
                Throws.TypeOf<FileNotFoundException>());
        }
    }
}
