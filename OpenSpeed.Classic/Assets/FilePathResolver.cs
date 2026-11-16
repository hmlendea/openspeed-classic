using System;
using System.IO;

using NuciLog.Core;

using OpenSpeed.Classic.Logging;

namespace OpenSpeed.Classic.Assets
{
    public sealed class FilePathResolver(ILogger logger) : IFilePathResolver
    {
        public string? ResolveFile(string rootDirectory, string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

            return ResolveFileAndLog(rootDirectory, relativePath);
        }

        public string? ResolveFile(
            string overridesDirectory,
            string rootDirectory,
            string relativePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(overridesDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

            string? resolvedPath = ResolveFileAndLog(
                overridesDirectory,
                relativePath);

            if (resolvedPath is not null)
            {
                return resolvedPath;
            }

            return ResolveFileAndLog(rootDirectory, relativePath);
        }

        private string? ResolveFileAndLog(
            string rootDirectory,
            string relativePath)
        {
            string? resolvedPath = ResolveFileWithinRoot(rootDirectory, relativePath);

            if (resolvedPath is not null)
            {
                logger.Info(
                    OpenSpeedOperation.ResolveAssetFile,
                    OperationStatus.Success,
                    new LogInfo(OpenSpeedLogInfoKey.FilePath, resolvedPath));

                return resolvedPath;
            }

            logger.Warn(
                OpenSpeedOperation.ResolveAssetFile,
                OperationStatus.Failure,
                new LogInfo(
                    OpenSpeedLogInfoKey.FilePath,
                    Path.Combine(rootDirectory, relativePath)));

            return null;
        }

        private static string? ResolveFileWithinRoot(
            string rootDirectory,
            string relativePath)
        {
            if (!Directory.Exists(rootDirectory) || Path.IsPathRooted(relativePath))
            {
                return null;
            }

            string[] pathComponents = relativePath.Split(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                StringSplitOptions.RemoveEmptyEntries);

            if (pathComponents.Length == 0)
            {
                return null;
            }

            string currentPath = Path.GetFullPath(rootDirectory);

            for (int componentIndex = 0;
                componentIndex < pathComponents.Length;
                componentIndex += 1)
            {
                string pathComponent = pathComponents[componentIndex];

                if (string.Equals(pathComponent, ".", StringComparison.Ordinal) ||
                    string.Equals(pathComponent, "..", StringComparison.Ordinal))
                {
                    return null;
                }

                bool isFileComponent = componentIndex == pathComponents.Length - 1;
                string exactPath = Path.Combine(currentPath, pathComponent);

                if (isFileComponent && File.Exists(exactPath))
                {
                    return exactPath;
                }

                if (!isFileComponent && Directory.Exists(exactPath))
                {
                    currentPath = exactPath;

                    continue;
                }

                string? matchingPath = FindMatchingPath(
                    currentPath,
                    pathComponent,
                    isFileComponent);

                if (matchingPath is null)
                {
                    return null;
                }

                if (isFileComponent)
                {
                    return matchingPath;
                }

                currentPath = matchingPath;
            }

            return null;
        }

        private static string? FindMatchingPath(
            string directoryPath,
            string pathComponent,
            bool isFileComponent)
        {
            foreach (string candidatePath in Directory.EnumerateFileSystemEntries(directoryPath))
            {
                if (!string.Equals(
                        Path.GetFileName(candidatePath),
                        pathComponent,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (isFileComponent && File.Exists(candidatePath))
                {
                    return candidatePath;
                }

                if (!isFileComponent && Directory.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            return null;
        }
    }
}