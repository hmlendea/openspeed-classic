namespace OpenSpeed.Classic.Tracks.Loading
{
    public interface ITrackLoadingService
    {
        public LoadedTrack Load(TrackLoadRequest request);
    }
}