namespace OpenSpeed.Classic.Configuration
{
    public interface IApplicationSettingsLoader
    {
        public ApplicationSettings Load(string filePath);
    }
}