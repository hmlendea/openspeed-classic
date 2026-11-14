using Microsoft.Xna.Framework;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;
using OpenSpeed.Classic.Rendering.Cars;

namespace OpenSpeed.Classic.UnitTests.Rendering.Cars
{
    [TestFixture]
    public sealed class PhysicsRenderAdapterTests
    {
        [Test]
        public void GivenNativeMotion_WhenConvertingForRendering_ThenCoordinatesAreConvertedWithoutStateMutation()
        {
            CarMemory car = new();
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            car[0x9C] = 4 * X86Math.One;
            car[0xA0] = 8 * X86Math.One;
            car[0xA4] = 16 * X86Math.One;
            car[0xB0] = 32 * X86Math.One;
            byte[] original = car.Snapshot();
            Matrix world = PhysicsRenderAdapter.CreateWorld(car);

            Assert.Multiple(() =>
            {
                Assert.That(world.Translation, Is.EqualTo(new Vector3(4, 8, -16)));
                Assert.That(world.Forward, Is.EqualTo(Vector3.Forward));
                Assert.That(world.Up, Is.EqualTo(Vector3.Up));
                Assert.That(PhysicsRenderAdapter.GetLongitudinalVelocity(car), Is.EqualTo(32.0f));
                Assert.That(car.Snapshot(), Is.EqualTo(original));
            });
        }
    }
}