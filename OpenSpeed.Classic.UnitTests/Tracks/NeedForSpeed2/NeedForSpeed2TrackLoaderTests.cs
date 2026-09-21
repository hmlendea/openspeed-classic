using System;
using System.IO;
using System.Linq;

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
            trackLoader = new NeedForSpeed2TrackLoader(new FilePathResolver());
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
            LoadedTrack track = trackLoader.Load(testDirectory, "outback");
            TrackBlock block = track.Blocks.Single();
            TrackSurface surface = block.Surfaces.Single();
            TrackSurface[] blockScenery = block.ScenerySurfaces.ToArray();
            TrackSurface globalScenery = track.ScenerySurfaces.Single();
            TrackPoint[] points = surface.Points.ToArray();
            TrackMaterial material = track.Materials.Single();
            TrackTexture texture = track.Textures.Single();
            TrackColour pixel = texture.Pixels.Single();

            Assert.Multiple(() =>
            {
                Assert.That(track.Identifier, Is.EqualTo("Outback"));
                Assert.That(track.DisplayName, Is.EqualTo("Outback"));
                Assert.That(track.Game, Is.EqualTo(GameVersion.NeedForSpeed2SpecialEdition));
                Assert.That(track.SourceFiles.Count(), Is.EqualTo(5));
                Assert.That(track.Horizon, Is.Not.Null);
                Assert.That(track.Horizon!.RingRadius, Is.EqualTo(1500));
                Assert.That(track.Horizon.SkyTexture!.Name, Is.EqualTo("CLD2"));
                Assert.That(block.Identifier, Is.Zero);
                Assert.That(block.Centre.X, Is.EqualTo(4.0));
                Assert.That(block.Centre.Y, Is.EqualTo(8.0));
                Assert.That(block.Centre.Z, Is.EqualTo(-16.0));
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