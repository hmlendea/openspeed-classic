namespace OpenSpeed.Classic.Physics
{
    public static class PendingCollisionEventConsumer
    {
        public static void Consume(CarMemory car)
        {
            if (car[0x578] > 0)
            {
                car[0x578] = unchecked(car[0x578] - 1);
            }

            car[0x164] = 0;
            car[0x168] = 0;
            car[0x160] = 0;
        }
    }
}