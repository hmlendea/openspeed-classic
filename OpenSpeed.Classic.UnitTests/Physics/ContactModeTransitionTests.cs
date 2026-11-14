using System;
using System.Collections.Generic;

using NUnit.Framework;

using OpenSpeed.Classic.Physics;

namespace OpenSpeed.Classic.UnitTests.Physics
{
    [TestFixture]
    public sealed class ContactModeTransitionTests
    {
        [TestCase(0, 0xF3FFFF, 0, false)]
        [TestCase(0, 0xF40000, 0, false)]
        [TestCase(0, 0xF40001, 1, true)]
        [TestCase(1, 0xF40001, 1, false)]
        [TestCase(2, 0xF40001, 1, true)]
        public void GivenDistanceAndMode_WhenUpdating_ThenOnlyTheNativeEntryStoresAreApplied(
            byte mode,
            int distance,
            byte expectedMode,
            bool doesResetSurface)
        {
            CarMemory car = BuildCar(mode);
            CarMemory expected = BuildCar(mode);
            expected[0x88] = distance;
            expected.WriteByte(0x8C, expectedMode);

            if (expectedMode == 1)
            {
                expected[0xE8] = 0;
                expected[0xF0] = 0;
            }

            if (doesResetSurface)
            {
                expected[0x180] = 1;
                expected[0x184] = 1;
            }

            int calls = 0;
            ContactModeTransition.Update(
                car, 1,
                body =>
                {
                    Assert.That(body, Is.SameAs(car));
                    calls += 1;

                    return distance;
                },
                body => Assert.Fail("No route query is expected."),
                body => Assert.Fail("No basis refresh is expected."),
                body => Assert.Fail("No contact reset is expected."));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
            }

        }

        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void GivenMoreThanTwoActiveCars_WhenUpdating_ThenOnlyModeAndDistanceAreCleared(int count)
        {
            CarMemory car = BuildCar(1);
            CarMemory expected = BuildCar(0);
            expected[0x88] = 0;

            ContactModeTransition.Update(
                car, count,
                body => throw new AssertionException("No distance query is expected."),
                body => Assert.Fail("No route query is expected."),
                body => Assert.Fail("No basis refresh is expected."),
                body => Assert.Fail("No contact reset is expected."));

            Assert.That(car.Snapshot(), Is.EqualTo(expected.Snapshot()));
        }

        [TestCase(1, 0xF3FFFF)]
        [TestCase(255, 0xF40000)]
        public void GivenALeavingBody_WhenUpdating_ThenQueriesPrecedeHeightCorrectionAndRecoveryReset(
            byte mode,
            int distance)
        {
            CarMemory car = BuildCar(mode);
            car[0x10C] = X86Math.One;
            FixedMatrices.Write(car, 0xC4, FixedMatrices.Identity());
            List<string> calls = [];

            ContactModeTransition.Update(
                car, 2, body => distance,
                body =>
                {
                    Assert.That(body.ReadByte(0x8C), Is.Zero);
                    calls.Add("query");
                },
                body => calls.Add("basis"),
                body =>
                {
                    calls.Add("reset");
                    body[0x128] = X86Math.One;
                    body[0x140] = 0x40000;
                });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(calls, Is.EqualTo(new[] { "query", "basis", "reset" }));
                Assert.That(car.ReadByte(0x8C), Is.Zero);
                Assert.That(car[0x88], Is.EqualTo(distance));
                Assert.That(car[0x154], Is.EqualTo(0x40000));
                Assert.That(car[0xA0], Is.EqualTo(0x50000));
                Assert.That(car[0x180], Is.EqualTo(1));
                Assert.That(car[0x184], Is.EqualTo(1));
                Assert.That(car[0x158], Is.Zero);
                Assert.That(car[0x160], Is.Zero);
                Assert.That(car[0x164], Is.Zero);
                Assert.That(car[0x168], Is.EqualTo(613));
                Assert.That(car[0x15C], Is.Zero);
                Assert.That(car[0x17C], Is.Zero);
                Assert.That(car[0x58C], Is.Zero);
                Assert.That(car[0xEC], Is.EqualTo(32));
            }

        }

        [Test]
        public void GivenAFailingExitQuery_WhenUpdating_ThenLaterStoresAndCallbacksDoNotExecute()
        {
            CarMemory car = BuildCar(1);
            AssertionException failure = new("The native query failed.");

            Assert.That(
                () => ContactModeTransition.Update(
                    car, 1, body => 0,
                    body => throw failure,
                    body => Assert.Fail("The basis refresh must not execute."),
                    body => Assert.Fail("The contact reset must not execute.")),
                Throws.Exception.SameAs(failure));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(car.ReadByte(0x8C), Is.Zero);
                Assert.That(car[0x88], Is.Zero);
                Assert.That(car[0x160], Is.EqualTo(32));
                Assert.That(car[0x17C], Is.EqualTo(42));
            }

        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void GivenAMissingDependency_WhenUpdating_ThenNoCarStateIsChanged(int missingDependency)
        {
            CarMemory car = BuildCar(1);
            byte[] expected = car.Snapshot();
            Func<CarMemory, int> calculateDistance = body => 0;
            Action<CarMemory> refreshQuery = body => Assert.Fail("No query is expected.");
            Action<CarMemory> refreshBasis = body => Assert.Fail("No basis refresh is expected.");
            Action<CarMemory> resetContact = body => Assert.Fail("No reset is expected.");

            if (missingDependency == 1)
            {
                calculateDistance = null!;
            }

            if (missingDependency == 2)
            {
                refreshQuery = null!;
            }

            if (missingDependency == 3)
            {
                refreshBasis = null!;
            }

            if (missingDependency == 4)
            {
                resetContact = null!;
            }

            CarMemory suppliedCar = car;

            if (missingDependency == 0)
            {
                suppliedCar = null!;
            }

            Assert.That(
                () => ContactModeTransition.Update(
                    suppliedCar, 1, calculateDistance, refreshQuery, refreshBasis, resetContact),
                Throws.ArgumentNullException);
            Assert.That(car.Snapshot(), Is.EqualTo(expected));
        }

        private static CarMemory BuildCar(byte mode)
        {
            CarMemory car = new();
            car.WriteByte(0x8C, mode);
            car[0x88] = 613;
            car[0xE8] = 42;
            car[0xEC] = 32;
            car[0xF0] = -42;
            car[0x180] = 8;
            car[0x184] = 16;
            car[0x158] = 42;
            car[0x160] = 32;
            car[0x164] = 16;
            car[0x168] = 613;
            car[0x17C] = 42;
            car[0x58C] = 32;

            return car;
        }
    }
}