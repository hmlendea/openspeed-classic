using System.Collections.Generic;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ForceDispatcherTests
    {
        [TestCase(0x1999, 0, "collision")]
        [TestCase(0x199A, 0, "collision,main")]
        [TestCase(0x199B, 1, "collision,alternate")]
        public void GivenDispatcherBoundaries_WhenUpdating_ThenTheExactCallbackOrderIsUsed(int projection, int mode, string expected)
        {
            CarMemory car = new();
            car[0x100] = projection;
            car.WriteByte(0x2DD, 1);
            PhysicsContext context = new() { CollisionFlags = 8, Mode = mode };
            List<string> calls = [];
            ForceDispatcher.Update(car, context, vehicle => calls.Add("collision"),
                vehicle => calls.Add("main"), vehicle => calls.Add("alternate"));

            Assert.That(string.Join(',', calls), Is.EqualTo(expected));
        }

        [TestCase(0x1F3, 0)]
        [TestCase(0x1F4, 0)]
        [TestCase(0x1F5, 1)]
        public void GivenTheLowProjectionBranch_WhenDecaying_ThenOnlySpecifiedFieldsChange(int engine, int expectedEngine)
        {
            CarMemory car = new();
            car[0x2F0] = engine;
            car[0x310] = car[0x324] = car[0x328] = car[0x2F4] = 42;
            car[0xA8] = 0x10000;
            car[0xAC] = -0x10000;
            car[0xB0] = 0x20000;
            car[0xE8] = car[0xEC] = car[0xF0] = 0x10000;
            ForceDispatcher.Update(car, new PhysicsContext(), UnexpectedCallback, UnexpectedCallback, UnexpectedCallback);
            CarMemory expected = new();
            expected[0x2F0] = expectedEngine;
            expected[0xA8] = 0xF0A3;
            expected[0xAC] = -0xF0A3;
            expected[0xB0] = 0x1E146;
            expected[0xE8] = expected[0xF0] = 0x10000;
            expected[0xEC] = 0xFAE1;

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [TestCase(0x7FFF, 0xF0A3)]
        [TestCase(0x8000, 0x10000)]
        [TestCase(0x8001, 0x10000)]
        public void GivenTheContactCorrectionGate_WhenUpdating_ThenDampingUsesTheStrictBoundary(int correction, int expectedVelocity)
        {
            CarMemory car = new();
            car[0x15C] = correction;
            car[0xA8] = X86Math.One;
            ForceDispatcher.Update(car, new PhysicsContext(), UnexpectedCallback, UnexpectedCallback, UnexpectedCallback);

            Assert.That(car[0xA8], Is.EqualTo(expectedVelocity));
        }

        private static void UnexpectedCallback(CarMemory car)
            => Assert.Fail("The low-projection path must not invoke a solver or collision callback.");
    }
}