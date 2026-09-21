namespace OpenSpeed.Classic.Assets
{
    public interface IFilePathResolver
    {
        public string? ResolveFile(string rootDirectory, string relativePath);
    }
}