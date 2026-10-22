namespace OpenSpeed.Classic.Rendering
{
    public sealed class SpeedometerReading(
        float dialRatio,
        int speedKilometresPerHour)
    {
        public float DialRatio { get; } = dialRatio;

        public int SpeedKilometresPerHour { get; } = speedKilometresPerHour;
    }
}