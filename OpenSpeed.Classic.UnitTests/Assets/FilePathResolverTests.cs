using System;
using System.IO;

using NUnit.Framework;

using OpenSpeed.Classic.Assets;

namespace OpenSpeed.Classic.UnitTests.Assets
{
    [TestFixture]
    public sealed class FilePathResolverTests
    {
        private IFilePathResolver filePathResolver = null!;
        private string testDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            filePathResolver = new FilePathResolver();
            testDirectory = Path.Combine(
                Path.GetTempPath(),
                $"openspeed-assets-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(testDirectory, "GameData", "Tracks"));
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
        public void GivenAnExactRelativePath_WhenResolvingAFile_ThenTheFileIsReturned()
        {
            string expectedPath = Path.Combine(testDirectory, "GameData", "Tracks", "TR02.TRK");
            File.WriteAllBytes(expectedPath, [4, 8, 16]);

            string? resolvedPath = filePathResolver.ResolveFile(
                testDirectory,
                Path.Combine("GameData", "Tracks", "TR02.TRK"));

            Assert.That(resolvedPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void GivenAMixedCaseRelativePath_WhenResolvingAFile_ThenTheFileIsReturned()
        {
            string expectedPath = Path.Combine(testDirectory, "GameData", "Tracks", "TR02.TRK");
            File.WriteAllBytes(expectedPath, [4, 8, 16]);

            string? resolvedPath = filePathResolver.ResolveFile(
                testDirectory,
                Path.Combine("gamedata", "tracks", "tr02.trk"));

            Assert.That(resolvedPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void GivenARootedRelativePath_WhenResolvingAFile_ThenNoFileIsReturned()
            => Assert.That(
                filePathResolver.ResolveFile(testDirectory, Path.GetFullPath("TR02.TRK")),
                Is.Null);

        [Test]
        public void GivenAParentTraversal_WhenResolvingAFile_ThenNoFileIsReturned()
            => Assert.That(
                filePathResolver.ResolveFile(
                    testDirectory,
                    Path.Combine("GameData", "..", "TR02.TRK")),
                Is.Null);

        [Test]
        public void GivenAMissingRoot_WhenResolvingAFile_ThenNoFileIsReturned()
            => Assert.That(
                filePathResolver.ResolveFile(
                    Path.Combine(testDirectory, "Absent"),
                    "TR02.TRK"),
                Is.Null);
    }
}