namespace OpenSpeed.Classic.Rendering
{
    public sealed class SpeedometerReading
    {
        public float DialRatio { get; }

        public int SpeedKilometresPerHour { get; }

        public SpeedometerReading(
            float dialRatio,
            int speedKilometresPerHour)
        {
            DialRatio = dialRatio;
            SpeedKilometresPerHour = speedKilometresPerHour;
        }
    }
}