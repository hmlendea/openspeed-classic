namespace OpenSpeed.Classic.Input
{
    public sealed class TrackCameraInput
    {
        public TrackCameraView CameraView { get; set; }

        public bool IsHandbrakeApplied { get; set; }

        public float MovementInput { get; set; }

        public float TurningInput { get; set; }
    }
}