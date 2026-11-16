namespace OpenSpeed.Classic.Assets
{
    public interface IFilePathResolver
    {
        public string? ResolveFile(string rootDirectory, string relativePath);

        public string? ResolveFile(
            string overridesDirectory,
            string rootDirectory,
            string relativePath);
    }
}