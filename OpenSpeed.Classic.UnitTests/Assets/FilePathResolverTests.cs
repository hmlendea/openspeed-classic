using System;
using System.IO;

using Moq;

using NuciLog.Core;

using NUnit.Framework;

using OpenSpeed.Classic.Assets;
using OpenSpeed.Classic.Logging;

namespace OpenSpeed.Classic.UnitTests.Assets
{
    [TestFixture]
    public sealed class FilePathResolverTests
    {
        private IFilePathResolver filePathResolver = null!;
        private Mock<ILogger> logger = null!;
        private string testDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            logger = new Mock<ILogger>();
            filePathResolver = new FilePathResolver(logger.Object);
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
            VerifyLog(LogLevel.Info, OperationStatus.Success, expectedPath);
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
        public void GivenAFileInBothDirectories_WhenResolvingAFile_ThenTheOverrideIsReturned()
        {
            string overridesDirectory = Path.Combine(testDirectory, "Overrides");
            string overrideTrackDirectory = Path.Combine(
                overridesDirectory,
                "GameData",
                "Tracks");
            Directory.CreateDirectory(overrideTrackDirectory);
            string rootPath = Path.Combine(testDirectory, "GameData", "Tracks", "TR02.TRK");
            string overridePath = Path.Combine(overrideTrackDirectory, "TR02.TRK");
            File.WriteAllBytes(rootPath, [4, 8, 16]);
            File.WriteAllBytes(overridePath, [32, 42, 48]);

            string? resolvedPath = filePathResolver.ResolveFile(
                overridesDirectory,
                testDirectory,
                Path.Combine("GameData", "Tracks", "TR02.TRK"));

            Assert.That(resolvedPath, Is.EqualTo(overridePath));
            VerifyLog(LogLevel.Info, OperationStatus.Success, overridePath);
        }

        [Test]
        public void GivenAFileMissingFromOverrides_WhenResolvingAFile_ThenTheRootFileIsReturned()
        {
            string overridesDirectory = Path.Combine(testDirectory, "Overrides");
            Directory.CreateDirectory(overridesDirectory);
            string expectedPath = Path.Combine(testDirectory, "GameData", "Tracks", "TR02.TRK");
            File.WriteAllBytes(expectedPath, [4, 8, 16]);

            string? resolvedPath = filePathResolver.ResolveFile(
                overridesDirectory,
                testDirectory,
                Path.Combine("GameData", "Tracks", "TR02.TRK"));

            Assert.That(resolvedPath, Is.EqualTo(expectedPath));
            VerifyLog(
                LogLevel.Warn,
                OperationStatus.Failure,
                Path.Combine(overridesDirectory, "GameData", "Tracks", "TR02.TRK"));
            VerifyLog(LogLevel.Info, OperationStatus.Success, expectedPath);
        }

        [Test]
        public void GivenAFileMissingFromBothDirectories_WhenResolvingAFile_ThenBothMissesAreLogged()
        {
            string overridesDirectory = Path.Combine(testDirectory, "Overrides");
            Directory.CreateDirectory(overridesDirectory);
            string relativePath = Path.Combine("GameData", "Tracks", "TR03.TRK");

            string? resolvedPath = filePathResolver.ResolveFile(
                overridesDirectory,
                testDirectory,
                relativePath);

            Assert.That(resolvedPath, Is.Null);
            VerifyLog(
                LogLevel.Warn,
                OperationStatus.Failure,
                Path.Combine(overridesDirectory, relativePath));
            VerifyLog(
                LogLevel.Warn,
                OperationStatus.Failure,
                Path.Combine(testDirectory, relativePath));
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

        private void VerifyLog(
            LogLevel level,
            OperationStatus operationStatus,
            string filePath)
        {
            if (Equals(level, LogLevel.Info))
            {
                logger.Verify(loggerInstance => loggerInstance.Info(
                    It.Is<Operation>(operation => operation.Name == "ResolveAssetFile"),
                    It.Is<OperationStatus>(status => status.Name == operationStatus.Name),
                    It.Is<LogInfo[]>(logInfos =>
                        logInfos.Length == 1 &&
                        logInfos[0].Key.Name == OpenSpeedLogInfoKey.FilePath.Name &&
                        logInfos[0].Value == filePath)),
                    Times.Once);

                return;
            }

            logger.Verify(loggerInstance => loggerInstance.Warn(
                It.Is<Operation>(operation => operation.Name == "ResolveAssetFile"),
                It.Is<OperationStatus>(status => status.Name == operationStatus.Name),
                It.Is<LogInfo[]>(logInfos =>
                    logInfos.Length == 1 &&
                    logInfos[0].Key.Name == OpenSpeedLogInfoKey.FilePath.Name &&
                    logInfos[0].Value == filePath)),
                Times.Once);
        }
    }
}