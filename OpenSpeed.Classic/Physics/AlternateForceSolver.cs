namespace OpenSpeed.Classic.Physics
{
    public static class AlternateForceSolver
    {
        public static void Update(
            CarMemory car,
            CarSpecifications descriptor,
            CarRuntimeType runtimeType,
            PhysicsContext context,
            PhysicsRoute route)
            => MainForceSolver.Update(car, descriptor, runtimeType, context, route, true);
    }
}